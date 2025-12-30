using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using Microsoft.PowerBI.Api.Models;
using Org.BouncyCastle.Ocsp;
using System.IO;

namespace BIEmbedSystem.API.Jobs
{
    public class CapacityScheduleMonitor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CapacityScheduleMonitor> _logger;

        private DateTime _lastSubscriptionRun = DateTime.MinValue;
        private readonly TimeSpan _subscriptionInterval = TimeSpan.FromSeconds(10);

        public CapacityScheduleMonitor(
            IServiceProvider serviceProvider,
            ILogger<CapacityScheduleMonitor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ CapacityScheduleMonitor started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<MDMDbContext>();
                    var schedulerService = scope.ServiceProvider.GetRequiredService<SchedulerService>();
                    var reportService = scope.ServiceProvider.GetRequiredService<ReportPbiEmbedService>();
                    var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                    // 🔹 1. Capacity jobs (can run every loop)
                    var activeSchedules = await db.Capacity_Scheduler
                        .Where(s => s.Status == "Active")
                        .ToListAsync(stoppingToken);

                    if (activeSchedules.Any())
                        await schedulerService.ScheduleCapacityJobsAsync(activeSchedules);

                    // 🔹 2. Subscriptions — ONLY every 10 seconds
                    if (DateTime.UtcNow - _lastSubscriptionRun >= _subscriptionInterval)
                    {
                        _lastSubscriptionRun = DateTime.UtcNow;
                        await RunSubscriptionsCronAsync(db, reportService, emailService);
                        await RunSemanticSchedulersCronAsync(db, reportService);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error while executing schedule monitor.");
                }

                await Task.Delay(1000, stoppingToken); // 1-sec loop tick
            }

            _logger.LogInformation("🛑 CapacityScheduleMonitor stopped.");
        }

        // =====================================================
        // 🔥 RUN SUBSCRIPTIONS (IST SAFE)
        // =====================================================
        private async Task RunSubscriptionsCronAsync(
            MDMDbContext db,
            ReportPbiEmbedService powerBiService,
            EmailService emailService)
        {
            var nowIst = TimeHelper.IstNow();

            var activeSubs = await db.ReportSubscription
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var sub in activeSubs)
            {
                // 🔹 Date range validation (IST)
                if (nowIst.Date < sub.ScheduleStartDate.Date)
                    continue;

                if (sub.ScheduleEndDate != null &&
                    nowIst.Date > sub.ScheduleEndDate.Value.Date)
                    continue;

                // 🔹 Convert AM/PM → 24-hour
                int hour = sub.ScheduleAMPM == "PM" && sub.ScheduleHour != 12
                    ? sub.ScheduleHour + 12
                    : sub.ScheduleHour;

                if (sub.ScheduleAMPM == "AM" && sub.ScheduleHour == 12)
                    hour = 0;

                var scheduledTime = new DateTime(
                    nowIst.Year,
                    nowIst.Month,
                    nowIst.Day,
                    hour,
                    sub.ScheduleMinute,
                    0
                );

                var currentTime = new DateTime(
                    nowIst.Year,
                    nowIst.Month,
                    nowIst.Day,
                    nowIst.Hour,
                    nowIst.Minute,
                    0
                );

                // 🔹 Repeat logic
                if (sub.RepeatType == "Weekly" &&
                    nowIst.DayOfWeek != DayOfWeek.Monday)
                    continue;

                if (sub.RepeatType == "Monthly")
                {
                    bool isLastDay =
                        nowIst.Day == DateTime.DaysInMonth(nowIst.Year, nowIst.Month);

                    if (!isLastDay)
                        continue;
                }

                // 🔹 Time must match EXACT minute
                if (scheduledTime != currentTime)
                    continue;

                // 🔹 Prevent duplicate execution in same minute
                if (sub.LastRunAt.HasValue &&
                    sub.LastRunAt.Value.Date == nowIst.Date &&
                    sub.LastRunAt.Value.Hour == nowIst.Hour &&
                    sub.LastRunAt.Value.Minute == nowIst.Minute)
                {
                    continue;
                }

                _logger.LogInformation($"📨 Running subscription: {sub.SubscriptionName}");

                // 🔹 Export report
                var stream = await powerBiService.ExportReportAsync(
                    sub.WorkspaceId,
                    sub.ReportId,
                    new ExportReportRequest { Format = (FileFormat)1 });

                byte[] pdfBytes;
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    pdfBytes = ms.ToArray();
                }

                string body = $@"
                <div style='font-family:Segoe UI, Arial;'>
                    <h2>Power BI Scheduled Report</h2>
                    <p><b>Report Id:</b> {sub.ReportId}</p>
                    <a href='{sub.ShareLink}'
                       style='padding:10px 18px; background:#107C41; color:white;
                              text-decoration:none; border-radius:4px;'>
                       Open report
                    </a>
                </div>";

                await emailService.SendEmailWithAttachmentAsync(
                    sub.Recipients.Split(',').ToList(),
                    sub.SubscriptionName,
                    body,
                    pdfBytes,
                    "report.pdf");

                // 🔹 Save last run (IST)
                sub.LastRunAt = nowIst;
                await db.SaveChangesAsync();

                _logger.LogInformation($"📧 Sent to {sub.Recipients}");
            }
        }
        // =====================================================
        // 🔥 RUN SEMANTIC SCHEDULERS (IST SAFE)
        // =====================================================
        private async Task RunSemanticSchedulersCronAsync(
            MDMDbContext db,
            ReportPbiEmbedService powerBiService)
        {
            var nowIst = TimeHelper.IstNow();

            var activeSchedulers = await db.SemanticSchedulers
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var scheduler in activeSchedulers)
            {
                // 🔹 Date window
                if (nowIst.Date < scheduler.ScheduleStartDate.Date)
                    continue;

                if (scheduler.ScheduleEndDate != null &&
                    nowIst.Date > scheduler.ScheduleEndDate.Value.Date)
                    continue;

                // 🔹 Convert AM/PM → 24-hour
                int hour = scheduler.ScheduleAMPM == "PM" && scheduler.ScheduleHour != 12
                    ? scheduler.ScheduleHour + 12
                    : scheduler.ScheduleHour;

                if (scheduler.ScheduleAMPM == "AM" && scheduler.ScheduleHour == 12)
                    hour = 0;

                var scheduledTime = new DateTime(
                    nowIst.Year,
                    nowIst.Month,
                    nowIst.Day,
                    hour,
                    scheduler.ScheduleMinute,
                    0
                );

                var currentTime = new DateTime(
                    nowIst.Year,
                    nowIst.Month,
                    nowIst.Day,
                    nowIst.Hour,
                    nowIst.Minute,
                    0
                );

                // 🔹 Repeat logic
                if (scheduler.RepeatType == "Weekly" &&
                    nowIst.DayOfWeek != DayOfWeek.Monday)
                    continue;

                if (scheduler.RepeatType == "Monthly")
                {
                    bool isLastDay =
                        nowIst.Day == DateTime.DaysInMonth(nowIst.Year, nowIst.Month);

                    if (!isLastDay)
                        continue;
                }

                // 🔹 Exact minute match
                if (scheduledTime != currentTime)
                    continue;

                // 🔹 Prevent duplicate execution
                if (scheduler.LastRunAt.HasValue &&
                    scheduler.LastRunAt.Value.Date == nowIst.Date &&
                    scheduler.LastRunAt.Value.Hour == nowIst.Hour &&
                    scheduler.LastRunAt.Value.Minute == nowIst.Minute)
                {
                    continue;
                }

                try
                {
                    _logger.LogInformation(
                        $"🔄 Refreshing dataset {scheduler.DatasetId} (Scheduler: {scheduler.SchedulerName})");

                    // 🔹 Trigger dataset refresh
                    await powerBiService.GetDatasetRefresh(
                        scheduler.WorkspaceId,
                        scheduler.DatasetId.ToString()
                    );

                    scheduler.LastRunAt = nowIst;
                    scheduler.LastRunStatus = "Success";
                    scheduler.LastRunMessage = null;
                }
                catch (Exception ex)
                {
                    scheduler.LastRunAt = nowIst;
                    scheduler.LastRunStatus = "Failed";
                    scheduler.LastRunMessage = ex.Message;

                    _logger.LogError(
                        ex,
                        $"❌ Semantic refresh failed for Dataset {scheduler.DatasetId}");
                }

                await db.SaveChangesAsync();
            }
        }

    }
}

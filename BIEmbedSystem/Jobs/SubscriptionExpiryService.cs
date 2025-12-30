using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace BIEmbedSystem.API.Jobs
{
    public class SubscriptionExpiryService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<SubscriptionExpiryService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);
        private readonly int _notifyDaysBefore = 7; // notify 7 days before expire

        public SubscriptionExpiryService(IServiceProvider provider, ILogger<SubscriptionExpiryService> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SubscriptionExpiryService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _provider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<MDMDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<EmailService>(); // your existing mailer
                    var now = DateTime.UtcNow.Date;

                    // 1) Notify orgs with upcoming expiry in _notifyDaysBefore
                    var notifyDate = now.AddDays(_notifyDaysBefore);
                    var upcoming = await db.OrganizationSubscriptions
                        .Include(x => x.Organization)
                        .Where(x => x.IsActive && x.EndDate.Date == notifyDate)
                        .ToListAsync();

                    foreach (var s in upcoming)
                    {
                        var admins = await db.Users.Where(u => u.OrganizationId == s.OrganizationId && u.Role == 2).ToListAsync();
                        var to = admins.Select(a => a.Email).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();

                        if (to.Any())
                        {
                            var subject = $"Subscription expiring in {_notifyDaysBefore} days for {s.Organization.Name}";
                            var body = $"Your subscription (Plan: {s.PlanId}) for organization {s.Organization.Name} will expire on {s.EndDate:yyyy-MM-dd}. Please renew to avoid service disruption.";
                            await emailService.SendEmailAsync(to, subject, body);
                            _logger.LogInformation("Sent expiry notification for OrgSubscription {Id} to {Count} admins", s.OrgSubscriptionId, to.Count);
                        }
                    }

                    // 2) Handle expired subscriptions
                    var expired = await db.OrganizationSubscriptions
                        .Include(x => x.Organization)
                        .Where(x => x.IsActive && x.EndDate.Date < now)
                        .ToListAsync();

                    foreach (var s in expired)
                    {
                        _logger.LogInformation("Processing expired subscription OrgSubscriptionId={Id}", s.OrgSubscriptionId);

                        // Mark subscription inactive
                        s.IsActive = false;

                        // Deactivate users (policy: deactivate users under that org)
                        var users = await db.Users.Where(u => u.OrganizationId == s.OrganizationId && u.IsActive).ToListAsync();
                        foreach (var u in users)
                        {
                            u.IsActive = false;
                        }

                        // Optionally notify users/admins
                        var allEmails = users.Select(u => u.Email).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
                        var adminEmails = (await db.Users.Where(u => u.OrganizationId == s.OrganizationId && u.Role == 2).ToListAsync())
                                          .Select(a => a.Email).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();

                        var subjectExpired = $"Subscription expired for {s.Organization.Name}";
                        var bodyExpired = $"Your subscription ended on {s.EndDate:yyyy-MM-dd}. Users have been deactivated. Contact support to renew.";

                        var sendTo = adminEmails.Union(allEmails).Distinct().ToList();
                        if (sendTo.Any())
                        {
                            await emailService.SendEmailAsync(sendTo, subjectExpired, bodyExpired);
                            _logger.LogInformation("Sent expiry emails for OrgSubscription {Id} to {Count} recipients", s.OrgSubscriptionId, sendTo.Count);
                        }
                    }

                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SubscriptionExpiryService loop");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
            _logger.LogInformation("SubscriptionExpiryService stopped.");
        }
    }

}

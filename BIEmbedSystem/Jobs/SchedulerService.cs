using BIEmbedSystem.Core.Entities;
using Quartz;
using Quartz.Impl.Matchers;

namespace BIEmbedSystem.API.Jobs
{
    public class SchedulerService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<SchedulerService> _logger;

        public SchedulerService(ISchedulerFactory schedulerFactory, ILogger<SchedulerService> logger)
        {
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        public async Task ScheduleCapacityJobsAsync(IEnumerable<CapacitySchedulerModel> schedules)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var existingJobs = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals("capacity_jobs"));

            // 🧹 STEP 1 — REMOVE INACTIVE OR EXPIRED JOBS
            foreach (var jobKey in existingJobs)
            {
                var jobDetail = await scheduler.GetJobDetail(jobKey);
                var data = jobDetail?.JobDataMap;
                if (data == null) continue;

                var capacityName = data.GetString("capacityName");
                var endStr = data.GetString("end_time");
                DateTime.TryParse(endStr, out var endTime);

                var dbSchedule = schedules.FirstOrDefault(s => s.CapacityName == capacityName);

                if (endTime < DateTime.UtcNow)
                {
                    await scheduler.DeleteJob(jobKey);
                    _logger.LogInformation($"🧹 Deleted EXPIRED job {jobKey.Name} for {capacityName}");
                    continue;
                }

                if (dbSchedule == null || dbSchedule.Status != "Active")
                {
                    await scheduler.DeleteJob(jobKey);
                    _logger.LogInformation($"🧹 Deleted INACTIVE job {jobKey.Name} for {capacityName}");
                }
            }

            // 🧩 STEP 2 — CREATE OR ENSURE VALID JOBS
            foreach (var schedule in schedules)
            {
                if (schedule.Status != "Active") continue;


                var start_time = schedule.start_time;
                var now = DateTime.Now;
                var start = new DateTime(
                    now.Year,
                    now.Month,
                    now.Day,
                    start_time.Hour,
                    start_time.Minute,
                    0,              // seconds = 0
                    0,              // milliseconds = 0
                    now.Kind
                );
                var end_time = schedule.end_time ?? start.AddMinutes(schedule.duration);
                var end = new DateTime(
                    now.Year,
                    now.Month,
                    now.Day,
                    end_time.Hour,
                    end_time.Minute,
                    0,              // seconds = 0
                    0,              // milliseconds = 0
                    now.Kind
                );
                var current_time = new DateTime(
                    now.Year,
                    now.Month,
                    now.Day,
                    now.Hour,
                    now.Minute,
                    0,              // seconds = 0
                    0,              // milliseconds = 0
                    now.Kind
                );


                _logger.LogWarning($"⚠️ Time CHeck start {start}:  Now {current_time}");
                if (start < current_time) continue;
                if (end <= start)
                {
                    _logger.LogWarning($"⚠️ Invalid schedule for {schedule.CapacityName}: End <= Start");
                    continue;
                }


                var suspendJobKey = new JobKey($"job_suspend_{schedule.Id}", "capacity_jobs");
                var resumeJobKey = new JobKey($"job_resume_{schedule.Id}", "capacity_jobs");

                // 🔹 Build Suspend Job
                var suspendJob = JobBuilder.Create<CapacityOperationJob>()
                    .WithIdentity(suspendJobKey)
                    .UsingJobData("capacityName", schedule.CapacityName)
                    .UsingJobData("operation", "suspend")
                    .UsingJobData("end_time", end.ToString("O"))
                    .Build();

                var suspendTrigger = TriggerBuilder.Create()
                    .WithIdentity($"trigger_suspend_{schedule.Id}", "capacity_triggers")
                    .StartAt(start)
                    .WithSimpleSchedule(x => x.WithRepeatCount(0))
                    .Build();


                // ------------------------------
                // SUSPEND JOB CHECK
                // ------------------------------
                if (await scheduler.CheckExists(suspendJobKey))
                {
                    if (await JobExistsWithSameTriggerTime(scheduler, suspendJobKey, start))
                    {
                        _logger.LogInformation($"⏭ Suspend already scheduled for {schedule.CapacityName}. Skipping…");
                    }
                    else
                    {
                        await scheduler.DeleteJob(suspendJobKey);
                        await scheduler.ScheduleJob(suspendJob, suspendTrigger);
                        _logger.LogInformation($"🔄 Re-scheduled Suspend for {schedule.CapacityName}");
                    }
                }
                else
                {
                    await scheduler.ScheduleJob(suspendJob, suspendTrigger);
                    _logger.LogInformation($"🆕 Scheduled Suspend for {schedule.CapacityName}");
                }


                // 🔹 Build Resume Job
                var resumeJob = JobBuilder.Create<CapacityOperationJob>()
                    .WithIdentity(resumeJobKey)
                    .UsingJobData("capacityName", schedule.CapacityName)
                    .UsingJobData("operation", "resume")
                    .UsingJobData("end_time", end.ToString("O"))
                    .Build();

                var resumeTrigger = TriggerBuilder.Create()
                    .WithIdentity($"trigger_resume_{schedule.Id}", "capacity_triggers")
                    .StartAt(end)
                    .WithSimpleSchedule(x => x.WithRepeatCount(0))
                    .Build();


                // ------------------------------
                // RESUME JOB CHECK
                // ------------------------------
                if (await scheduler.CheckExists(resumeJobKey))
                {
                    if (await JobExistsWithSameTriggerTime(scheduler, resumeJobKey, end))
                    {
                        _logger.LogInformation($"⏭ Resume already scheduled for {schedule.CapacityName}. Skipping…");
                    }
                    else
                    {
                        await scheduler.DeleteJob(resumeJobKey);
                        await scheduler.ScheduleJob(resumeJob, resumeTrigger);
                        _logger.LogInformation($"🔄 Re-scheduled Resume for {schedule.CapacityName}");
                    }
                }
                else
                {
                    await scheduler.ScheduleJob(resumeJob, resumeTrigger);
                    _logger.LogInformation($"🆕 Scheduled Resume for {schedule.CapacityName}");
                }

                //// ⛔ ONLY CREATE IF NOT ALREADY VALID
                //await EnsureJobScheduledOnce(scheduler, suspendJobKey, suspendJob, suspendTrigger, start, schedule.CapacityName, "SUSPEND");
                //await EnsureJobScheduledOnce(scheduler, resumeJobKey, resumeJob, resumeTrigger, end, schedule.CapacityName, "RESUME");
            }

            if (!scheduler.IsStarted)
                await scheduler.Start();
        }

        private async Task EnsureJobScheduledOnce(
            IScheduler scheduler,
            JobKey jobKey,
            IJobDetail job,
            ITrigger trigger,
            DateTime fireTime,
            string capacityName,
            string label)
        {
            if (await scheduler.CheckExists(jobKey))
            {
                if (await TriggerMatchesTime(scheduler, jobKey, fireTime))
                {
                    _logger.LogInformation($"⏭️ {label} job already scheduled correctly for {capacityName}, skipping.");
                    return;
                }

                _logger.LogInformation($"♻️ Re-scheduling {label} job for {capacityName}");
                await scheduler.DeleteJob(jobKey);
            }

            await scheduler.ScheduleJob(job, trigger);
            _logger.LogInformation($"🆕 Scheduled {label} job for {capacityName} at {fireTime}");
        }

        private async Task<bool> TriggerMatchesTime(IScheduler scheduler, JobKey key, DateTime desired)
        {
            var triggers = await scheduler.GetTriggersOfJob(key);
            if (!triggers.Any()) return false;

            var next = triggers.First().GetNextFireTimeUtc()?.UtcDateTime;
            return next.HasValue && Math.Abs((next.Value - desired).TotalSeconds) < 1;
        }

        private async Task<bool> JobExistsWithSameTriggerTime(
        IScheduler scheduler,
        JobKey jobKey,
        DateTime expectedFireTime)
        {
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            if (triggers == null || !triggers.Any())
                return false;

            var trigger = triggers.First();
            var next = trigger.GetNextFireTimeUtc()?.UtcDateTime;

            return next.HasValue && Math.Abs((next.Value - expectedFireTime).TotalSeconds) < 1;
        }

    }
}

using System;
using System.Collections.Generic;

namespace BIEmbedSystem.Core.Entities
{
    public class RefreshableInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Kind { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int? RefreshCount { get; set; }
        public int? RefreshFailures { get; set; }
        public double? AverageDuration { get; set; }
        public double? MedianDuration { get; set; }
        public int? RefreshesPerDay { get; set; }

        public LastRefreshInfo? LastRefresh { get; set; }
        public RefreshScheduleInfo? RefreshSchedule { get; set; }

        public IList<string>? ConfiguredBy { get; set; }

        // Keep these for backward compatibility with existing code that used them
        public string? DatasetId { get; set; }
        public string? DatasetName { get; set; }
        public string? RefreshType { get; set; }
    }

    public class LastRefreshInfo
    {
        public string? RefreshType { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }
        public string? RequestId { get; set; }
    }

    public class RefreshScheduleInfo
    {
        public IList<string>? Days { get; set; }
        public IList<string>? Times { get; set; }
        public bool? Enabled { get; set; }
        public string? LocalTimeZoneId { get; set; }
        public string? NotifyOption { get; set; }
    }

    // Optional wrapper matching the JSON top-level shape: { "value": [ ... ] }
    public class Refreshables
    {
        public string? Odatacontext { get; set; }
        public IList<RefreshableInfo>? Value { get; set; }
    }
}

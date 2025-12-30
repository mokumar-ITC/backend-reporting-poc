public class ReportSubscriptionDto
{
    public int? Id { get; set; }   // nullable for create

    public Guid WorkspaceId { get; set; }
    public Guid ReportId { get; set; }

    public string SubscriptionName { get; set; }
    public List<string> Recipients { get; set; }

    public bool AttachFullReport { get; set; }

    public DateTime ScheduleStartDate { get; set; }
    public DateTime? ScheduleEndDate { get; set; }

    public string RepeatType { get; set; }

    public int ScheduleHour { get; set; }
    public int ScheduleMinute { get; set; }
    public string ScheduleAMPM { get; set; }

    public string TimeZone { get; set; }

    public bool IsActive { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public string? shareLink { get; set; }
}

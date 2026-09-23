namespace EPEAPI.Dto;

public class ReminderEscalationSettings
{
    /// <summary>How often the background service checks for stalled approvals (minutes)</summary>
    public int CheckIntervalMinutes { get; set; } = 5;

    /// <summary>Hours after the first email send before a reminder is sent</summary>
    public int DefaultReminderIntervalHours { get; set; } = 24;

    /// <summary>Hours after the first email send before escalation is triggered</summary>
    public int DefaultEscalationIntervalHours { get; set; } = 72;

    /// <summary>Maximum number of reminders before escalation</summary>
    public int DefaultMaxReminders { get; set; } = 2;

    /// <summary>Email address to notify when an escalation is triggered</summary>
    public string? EscalationNotifyEmail { get; set; }
}
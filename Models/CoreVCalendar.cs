using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class CoreVCalendar
{
    public DateOnly? AnalysisDate { get; set; }

    public int? AnalysisYear { get; set; }

    public int? AnalysisQuarter { get; set; }

    public int? AnalysisMonth { get; set; }

    public int? AnalysisWeek { get; set; }

    public string? AnalysisDateDisplay { get; set; }

    public string? AnalysisMonthDisplay { get; set; }

    public string? AnalysisWeekDisplay { get; set; }

    public string QuarterFormat { get; set; } = null!;

    public string? MonthFormat { get; set; }

    public string? WeekFormat { get; set; }

    public byte IsNoWork { get; set; }

    public string? Remarks { get; set; }

    public string? DateName { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }
}

using System;

namespace EPEAPI.Models;

public partial class EpeTransactionlog
{
    public long Id { get; set; }

    public long TransactionId { get; set; }

    public int PolicyId { get; set; }

    public string? TransactionType { get; set; }

    public int? PreviousStatus { get; set; }

    public int? NewStatus { get; set; }

    public int PerformedBy { get; set; }

    public DateTime PerformedAt { get; set; }

    public string? Remarks { get; set; }

    public string? Reason { get; set; }

    public string? Metadata { get; set; }

    public string? Workstation { get; set; }

    public string? IpAddress { get; set; }

    public string? MacAddress { get; set; }
}

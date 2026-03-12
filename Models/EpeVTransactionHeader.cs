using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class EpeVTransactionHeader
{
    public long TransactionId { get; set; }

    public string? TransactionNumber { get; set; }

    public string? RequesterId { get; set; }

    public string? RequesterName { get; set; }

    public string? Corporate { get; set; }

    public int PolicyId { get; set; }

    public string? PolicyName { get; set; }

    public string? Details { get; set; }

    public string Status { get; set; } = null!;

    public string? FinalDecision { get; set; }

    public string? Remark { get; set; }

    public byte IsDelete { get; set; }

    public int? CreatedByUserId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? ModifiedByUserId { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? Workstation { get; set; }

    public string? IpAddress { get; set; }

    public string? MacAddress { get; set; }
}

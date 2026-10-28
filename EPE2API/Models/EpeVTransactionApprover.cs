using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class EpeVTransactionApprover
{
    public long TransApproverId { get; set; }

    public long TransactionId { get; set; }

    public string? PolicyApproverId { get; set; }

    public string? EmployeeId { get; set; }

    public string? ApproverName { get; set; }

    public string? ApproverEmail { get; set; }

    public string? ApproverType { get; set; }

    public int? LineSequence { get; set; }

    public int? Sort { get; set; }

    public string? Remarks { get; set; }

    public int Respond { get; set; }

    public DateTime? RespondDate { get; set; }

    public byte IsSent { get; set; }

    public DateTime? SentDate { get; set; }

    public int SentCount { get; set; }

    public DateTime? LastSendDate { get; set; }

    public byte IsDelete { get; set; }

    public DateTime CreateDate { get; set; }
}

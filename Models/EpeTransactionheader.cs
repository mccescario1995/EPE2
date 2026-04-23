using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class EpeTransactionheader
{
    public long Transactionid { get; set; }

    public string? Transactionnumber { get; set; }

    public string? Requestorid { get; set; }

    public string? Corporate { get; set; }

    public int Policyid { get; set; }

    public string? Details { get; set; }

    /// <summary>
    /// Open, Pending, Closed
    /// </summary>
    public string? Status { get; set; }

    public string? Remarks { get; set; }

    public string? Finaldecision { get; set; }

    public byte Isdelete { get; set; }

    public int? Createdby { get; set; }

    public DateTime Createddate { get; set; }

    public int? Modifiedby { get; set; }

    public DateTime? Modifieddate { get; set; }

    public string? Workstation { get; set; }

    public string? Ipaddress { get; set; }

    public string? Macaddress { get; set; }
}

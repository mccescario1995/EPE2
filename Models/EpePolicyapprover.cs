using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class EpePolicyapprover
{
    public int Id { get; set; }

    public int Policyid { get; set; }

    public string? Approverid { get; set; }

    public string? Approvername { get; set; }

    public string? Approveremail { get; set; }

    /// <summary>
    /// Head,Manual
    /// </summary>
    public string? Approvertype { get; set; }

    public int? Sort { get; set; }

    public int? Linesequence { get; set; }

    public string Status { get; set; } = null!;

    public byte Isdelete { get; set; }

    public int? Createdby { get; set; }

    public DateTime Createddate { get; set; }

    public int? Modifiedby { get; set; }

    public DateTime? Modifieddate { get; set; }
}

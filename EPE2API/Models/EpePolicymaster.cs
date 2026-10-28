using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class EpePolicymaster
{
    public int Policyid { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// A, I
    /// </summary>
    public string Status { get; set; } = null!;

    public byte Isdelete { get; set; }

    public int? Createdby { get; set; }

    public DateTime Createddate { get; set; }

    public int? Modifiedby { get; set; }

    public DateTime? Modifieddate { get; set; }

    public string? Shortname { get; set; }
}

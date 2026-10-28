using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class EpeTransactionapprover
{
    public long Transapproverid { get; set; }

    public long Transactionid { get; set; }

    public string? Approverid { get; set; }

    public string? Approvertype { get; set; }

    public int? Linesequence { get; set; }

    public int? Sort { get; set; }

    public string? Remarks { get; set; }

    /// <summary>
    /// 0 = no answer, 1 = approved, 2 = disapproved
    /// </summary>
    public int Respond { get; set; }

    public DateTime? Responddate { get; set; }

    public byte Issent { get; set; }

    public DateTime? Sentdate { get; set; }

    public int Sentcount { get; set; }

    public DateTime? Lastsentdate { get; set; }

    public byte Isdelete { get; set; }

    public DateTime Createddate { get; set; }
}

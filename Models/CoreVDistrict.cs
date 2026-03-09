using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class CoreVDistrict
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? HeadId { get; set; }

    public string HeadName { get; set; } = null!;

    public string HeadContact { get; set; } = null!;

    public string? HeadEmail { get; set; }

    public int SortOrder { get; set; }

    public byte Status { get; set; }

    public string StatusName { get; set; } = null!;

    public string? CreatedByUserId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int? ModifiedByUserId { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }
}

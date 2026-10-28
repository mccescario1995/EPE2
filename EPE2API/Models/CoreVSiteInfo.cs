using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class CoreVSiteInfo
{
    public string SiteCode { get; set; } = null!;

    public string SiteName { get; set; } = null!;

    public string? SiteAlias { get; set; }

    public string SitePhoneName { get; set; } = null!;

    public string SiteHeadName { get; set; } = null!;

    public string SiteHeadIdno { get; set; } = null!;

    public string SiteEmailAdd { get; set; } = null!;

    public string Districtcode { get; set; } = null!;

    public string DistrictPhoneName { get; set; } = null!;

    public string Districtheadname { get; set; } = null!;

    public string? DistrictHeadIdno { get; set; }

    public string? DistrictEmailAdd { get; set; }

    public string? SiteMap { get; set; }

    public string? SiteLatitude { get; set; }

    public string? SiteLongitude { get; set; }

    public string Address { get; set; } = null!;

    public string BusinessUnit { get; set; } = null!;

    public byte Withpism { get; set; }

    public int? TotalAnalysisCode { get; set; }

    public string Districtname { get; set; } = null!;

    public byte ShowReport { get; set; }

    public string Hrisbranchcode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal? Matrix { get; set; }

    public decimal? Additives { get; set; }

    public decimal? RelieverRd { get; set; }

    public decimal? ProjbaseBill { get; set; }

    public decimal? SundayOps { get; set; }

    public decimal? RelieverAbsent { get; set; }

    public decimal? Nonmatrix { get; set; }

    public decimal? ProjbaseNotbill { get; set; }

    public byte? Isreportauto { get; set; }

    public byte? Isdepartment { get; set; }
}

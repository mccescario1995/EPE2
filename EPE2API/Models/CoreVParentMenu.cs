using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class CoreVParentMenu
{
    public int ParentId { get; set; }

    public string? ParentName { get; set; }

    public int? Parentmenuorder { get; set; }

    public string? Parenticon { get; set; }

    public int? UserId { get; set; }

    public int SysId { get; set; }

    public string Menucode { get; set; } = null!;
}

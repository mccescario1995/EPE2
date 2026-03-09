using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class CoreUserMenuList
{
    public int SysId { get; set; }

    public string? Sysname { get; set; }

    public int MenuId { get; set; }

    public string? Menuname { get; set; }

    public string Menuparent { get; set; } = null!;

    public bool? Isactive { get; set; }

    public int? Menuorder { get; set; }

    public string? Viewtype { get; set; }

    public string? Prtmenucode { get; set; }

    public bool? Istransaction { get; set; }

    public string? Menutype { get; set; }

    public string? Nenunames { get; set; }

    public int UserId { get; set; }

    public bool? Canadd { get; set; }

    public bool? Canedit { get; set; }

    public bool? Candelete { get; set; }

    public bool Canview { get; set; }

    public bool? Canopen { get; set; }

    public int Expr1 { get; set; }

    public int Subtotal { get; set; }

    public string? Menutypedes { get; set; }

    public bool? Forweb { get; set; }

    public int? ParentMenuOrder { get; set; }

    public string? Syscode { get; set; }

    public string? Sysdescription { get; set; }

    public string? ParentIcon { get; set; }

    public string? ChildIcon { get; set; }

    public bool? Formobile { get; set; }

    public string Menucode { get; set; } = null!;
}

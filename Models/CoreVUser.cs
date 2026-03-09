using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class CoreVUser
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Userrole { get; set; } = null!;

    public string? Userpass { get; set; }

    public string Lastname { get; set; } = null!;

    public string Middlename { get; set; } = null!;

    public string Firstname { get; set; } = null!;

    public string Employeename { get; set; } = null!;

    public string Employeename2 { get; set; } = null!;

    public string? Nickname { get; set; }

    public string Usertype { get; set; } = null!;

    public string? EmplId { get; set; }

    public string Emailaddress { get; set; } = null!;

    public string? Mobileno { get; set; }

    public string Status { get; set; } = null!;

    public string? Hashcode { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public bool Canaccessaquila { get; set; }

    public bool Ismailvalid { get; set; }

    public bool Isphonevalid { get; set; }

    public bool? Isverify { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace EPEAPI.Models;

public class ApproverRequest
{
    public int UserId { get; set; }
    public string? EmployeeId { get; set; }
    public string? Remarks { get; set; }
    public int Respond { get; set; } // 0 = no answer, 1 = approved, 2 = disapproved
}

public class ApproverResponseRequest
{
    public long TransactionId { get; set; }
    public long TransApproverId { get; set; }
    public string? EmployeeId { get; set; }
    public string? Remarks { get; set; }
    public int Respond { get; set; } // 1 = approved, 2 = disapproved
}



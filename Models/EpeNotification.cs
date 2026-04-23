// Models/EpeEmailApprovalToken.cs
namespace EPEAPI.Models;

public partial class EpeEmailApprovalToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public long TransactionId { get; set; }
    public long TransApproverId { get; set; }
    public string Action { get; set; } = null!; // "approve" | "disapprove"
    public bool IsUsed { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
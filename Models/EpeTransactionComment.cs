using System;

namespace EPEAPI.Models;

public partial class EpeTransactionComment
{
    public int Id { get; set; }

    public long TransactionId { get; set; }

    public int UserId { get; set; }

    public string EmployeeId { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = "Comment";

    public int? ParentCommentId { get; set; }

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Metadata { get; set; }
}
namespace EPEAPI.Dto;

using System.Text.Json.Serialization;

public class CreateCommentRequest
{
    public string Message { get; set; } = null!;

    public string Type { get; set; } = "Comment";

    public int? ParentCommentId { get; set; }

    public int UserId { get; set; }
}

public class UpdateCommentRequest
{
    public string? Message { get; set; } = null!;

    public int UserId { get; set; }
}

public class CommentQueryParams
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Type { get; set; }

    public bool? UnresolvedOnly { get; set; }
}

public class CommentResponse
{
    public int Id { get; set; }

    public long TransactionId { get; set; }

    public int UserId { get; set; }

    public string EmployeeId { get; set; } = null!;

    public string EmployeeName { get; set; } = null!;

    public string EmployeeEmail { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = "Comment";

    public int? ParentCommentId { get; set; }

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Metadata { get; set; }

    public bool IsCurrentUser { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }

    [JsonPropertyName("replies")]
    public List<CommentResponse> Replies { get; set; } = new();
}

public class CommentListResponse
{
    public bool Success { get; set; }

    public List<CommentResponse> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

        public int UnresolvedCount { get; set; }
        
        // Transaction status (O=Open, P=Pending, C=Closed, D=Draft)
        [JsonPropertyName("transactionStatus")]
        public string TransactionStatus { get; set; } = "O";
    }
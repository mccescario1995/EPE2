namespace EPEAPI.Dto;

public class CreatePolicyRequest
{
    public string? Description { get; set; }
    public string? Shortname { get; set; }
    public string Status { get; set; } = "A"; // Default active
}

public class ReorderApproverItem           // added this new DTO
{
    public int Id { get; set; }
    public int Sort { get; set; }
    public int Linesequence { get; set; }
}
namespace EPEAPI.Dto;

public class CreatePolicyRequest
{
    public string? Description { get; set; }
    public string? Shortname { get; set; }
    public string Status { get; set; } = "A"; // Default active
}
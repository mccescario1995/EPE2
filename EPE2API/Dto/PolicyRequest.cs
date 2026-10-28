namespace EPEAPI.Dto;

public class PolicyCreateRequest
{
    public int UserId { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "A";
    public string? Shortname { get; set; }
}
public class PolicyUpdateRequest
{
    public int PolicyId { get; set; }
    public int UserId { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "A";
    public string? Shortname { get; set; }
}

public class PolicyDeleteRequest
{
    public int UserId { get; set; }
    public int PolicyId { get; set; }
}
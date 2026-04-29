namespace EPEAPI.Dto;

public class CreateApproverRequest
{
    public int Policyid { get; set; }
    public string? Approverid { get; set; }
    public string? Approvername { get; set; }
    public string? Approveremail { get; set; }
    public string? Approvertype { get; set; }
    public int Sort { get; set; } = 0;
    public int Linesequence { get; set; } = 1;
    public int CreatedBy { get; set; } // Added
}
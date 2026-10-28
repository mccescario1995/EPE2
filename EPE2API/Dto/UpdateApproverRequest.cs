namespace EPEAPI.Dto;

public class UpdateApproverRequest
{
    public byte Isdelete { get; set; }
    public int Sort { get; set; }
    public int Linesequence { get; set; }
    public int ModifiedBy { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace EPEAPI.Dto;

public class CreateExemptionRequestDto
{
    public int Policyid { get; set; }
    public string? Details { get; set; }
    public string? Requestorid { get; set; }
    public int? Createdby { get; set; }
    public string? Ipaddress { get; set; }
    public string? Macaddress { get; set; }
    public List<AttachmentDto>? Attachments { get; set; }
}

public class DraftExemptionRequestDto
{
    public int? Policyid { get; set; }
    public string? Details { get; set; }
    public string? Requestorid { get; set; }
    public int? Modifiedby { get; set; }
    public string? Ipaddress { get; set; }

    public string? Macaddress { get; set; }

    public List<AttachmentDto>? Attachments { get; set; }
}

public class UploadAttachmentRequest
{
    public List<IFormFile> Files { get; set; }
}
public class AttachmentDto
{
    [Required]
    public IFormFile File { get; set; }
    // public string? ReferenceType { get; set; } = null;
}
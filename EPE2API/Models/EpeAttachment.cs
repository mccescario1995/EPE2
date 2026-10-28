using System;
using System.Collections.Generic;

namespace EPEAPI.Models;

public partial class EpeAttachment
{
    public int AttachId { get; set; }

    public string? TaskId { get; set; }

    public string? ReferenceType { get; set; }

    public string? UploadFilename { get; set; }

    public string? Newfilename { get; set; }

    public string? Filetype { get; set; }

    public DateTime Createdate { get; set; }

    public string? Computerhost { get; set; }
}

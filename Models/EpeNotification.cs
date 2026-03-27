using System;

namespace EPEAPI.Models
{
    public class EpeNotification
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, success, warning, error
        public bool IsRead { get; set; } = false;
        public string? RelatedUrl { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
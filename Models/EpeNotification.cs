using System;

namespace EPEAPI.Models
{
    public class EpeNotification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, success, warning, error
        public byte IsRead { get; set; }
        public string? RelatedUrl { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
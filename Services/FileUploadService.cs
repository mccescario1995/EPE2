using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EPEAPI.Services;

public class FileUploadService
{
    private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "attachments");

    public FileUploadService()
    {
        // Ensure the upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<(bool IsValid, string ErrorMessage)> ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return (false, "File is empty.");
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB
        {
            return (false, "File size exceeds 10MB limit.");
        }

        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return (false, "File type not allowed. Allowed types: PDF, DOC, DOCX, XLS, XLSX, TXT, JPG, JPEG, PNG.");
        }

        return (true, string.Empty);
    }

    public async Task<string> SaveFile(IFormFile file)
    {
        var validation = await ValidateFile(file);
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.ErrorMessage);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(_uploadPath, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return uniqueFileName;
    }
}
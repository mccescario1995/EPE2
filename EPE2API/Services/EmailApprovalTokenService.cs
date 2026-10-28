// Services/EmailApprovalTokenService.cs
using EPEAPI.Models;

namespace EPEAPI.Services;

public class EmailApprovalTokenService
{
    private readonly EpeDbContext _context;

    public EmailApprovalTokenService(EpeDbContext context)
    {
        _context = context;
    }

    public async Task<(string ApproveToken, string DisapproveToken)> GenerateTokensAsync(
        long transactionId,
        long transApproverId,
        int expiryHours = 72)
    {
        var approveToken = GenerateSecureToken();
        var disapproveToken = GenerateSecureToken();
        var expiresAt = DateTime.Now.AddHours(expiryHours);

        _context.EpeEmailApprovalTokens.AddRange(
            new EpeEmailApprovalToken
            {
                Token = approveToken,
                TransactionId = transactionId,
                TransApproverId = transApproverId,
                Action = "approve",
                IsUsed = false,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.Now
            },
            new EpeEmailApprovalToken
            {
                Token = disapproveToken,
                TransactionId = transactionId,
                TransApproverId = transApproverId,
                Action = "disapprove",
                IsUsed = false,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.Now
            }
        );

        await _context.SaveChangesAsync();
        return (approveToken, disapproveToken);
    }

    private static string GenerateSecureToken()
        => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-").Replace("/", "_").Replace("=", ""); // URL-safe base64
}
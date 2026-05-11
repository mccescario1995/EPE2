// Controllers/EmailApprovalController.cs
using EPEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EPEAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmailApprovalController : ControllerBase
{
    private readonly EpeDbContext _context;
    private readonly ILogger<EmailApprovalController> _logger;

    public EmailApprovalController(EpeDbContext context, ILogger<EmailApprovalController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /api/emailapproval/act?token=xxx
    // This is what the email button hits
    [HttpGet("act")]
    public async Task<IActionResult> ActOnToken([FromQuery] string token)
    {
        //var baseUrl = "https://apps.fastlogistics.com.ph/epe2dev"; // TODO: Move to config
        var baseUrl = "http://localhost:3000/epe2dev"; // For development, change to production URL when deploying

        try
        {
            var record = await _context.EpeEmailApprovalTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (record == null)
                return Redirect($"{baseUrl}/#/email-response?status=invalid");

            if (record.IsUsed)
                return Redirect($"{baseUrl}/#/email-response?status=already_used&transactionId={record.TransactionId}");

            if (record.ExpiresAt < DateTime.Now)
                return Redirect($"{baseUrl}/#/email-response?status=expired&transactionId={record.TransactionId}");

            // Get transaction
            var transaction = await _context.EpeTransactionheaders
                .FirstOrDefaultAsync(t => t.Transactionid == record.TransactionId );

            if (transaction == null)
                return Redirect($"{baseUrl}/#/email-response?status=not_found");

            if (transaction.Status == "C")
                return Redirect($"{baseUrl}/#/email-response?status=already_closed&transactionId={record.TransactionId}");

            // Get the specific approver record
            var approver = await _context.EpeTransactionapprovers
                .FirstOrDefaultAsync(a => a.Transactionid == transaction.Transactionid);

            if (approver == null)
                return Redirect($"{baseUrl}/#/email-response?status=invalid");

            if (approver.Respond != 0)
                return Redirect($"{baseUrl}/#/email-response?status=already_responded&transactionId={record.TransactionId}");

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var respond = record.Action == "approve" ? 1 : 2;

                // Mark token as used
                record.IsUsed = true;

                // Process the approval (reuse same logic as ExemptionController.RespondToApproval)
                await ProcessApproval(record.TransactionId, record.TransApproverId, respond, "Approved via email");

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                var action = record.Action == "approve" ? "approved" : "disapproved";
                return Redirect($"{baseUrl}/#/email-response?status=success&action={action}&transactionId={record.TransactionId}");
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                return Redirect($"{baseUrl}/#/email-response?status=error&transactionId={record.TransactionId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email approval token processing failed for token {Token}", token);
            return Redirect($"{baseUrl}/#/email-response?status=error");
        }
    }

    private async Task ProcessApproval(long transactionId, long transApproverId, int respond, string remarks)
    {
        var requester = await _context.EpeTransactionheaders
            .Where(t => t.Transactionid == transactionId)
            .Select(t => t.Requestorid)
            .FirstOrDefaultAsync();

        var requesterDetails = await _context.CoreVEmployeeDetails
            .FirstOrDefaultAsync(e => e.EmplId == requester);

        await _context.EpeTransactionapprovers
            .Where(a => a.Transapproverid == transApproverId)
            .ExecuteUpdateAsync(b => b
                .SetProperty(a => a.Respond, respond)
                .SetProperty(a => a.Responddate, DateTime.Now)
                .SetProperty(a => a.Remarks, remarks));

        if (respond == 1) // Approved
        {
            var allApprovers = await _context.EpeTransactionapprovers
                .Where(a => a.Transactionid == transactionId && a.Isdelete == 0)
                .OrderBy(a => a.Linesequence).ThenBy(a => a.Sort)
                .ToListAsync();

            var currentApprover = allApprovers.First(a => a.Transapproverid == transApproverId);

            // Auto-approve others in same sort group
            var groupIds = allApprovers
                .Where(a => a.Sort == currentApprover.Sort && a.Respond == 0)
                .Select(a => a.Transapproverid).ToList();

            if (groupIds.Any())
            {
                await _context.EpeTransactionapprovers
                    .Where(a => groupIds.Contains(a.Transapproverid))
                    .ExecuteUpdateAsync(b => b
                        .SetProperty(a => a.Respond, 1)
                        .SetProperty(a => a.Responddate, DateTime.Now)
                        .SetProperty(a => a.Remarks, "Auto-approved: group member approved via email"));
            }

            // Re-fetch to check next sequence
            var updatedApprovers = await _context.EpeTransactionapprovers
                .Where(a => a.Transactionid == transactionId && a.Isdelete == 0)
                .OrderBy(a => a.Linesequence).ThenBy(a => a.Sort)
                .ToListAsync();

            var currentGroupSequences = allApprovers
                .Where(a => a.Sort == currentApprover.Sort)
                .Select(a => a.Linesequence).Distinct().ToHashSet();

            var nextApprovers = updatedApprovers
                .Where(a => !currentGroupSequences.Contains(a.Linesequence)
                         && a.Linesequence > currentApprover.Linesequence
                         && a.Respond == 0)
                .ToList();

            if (nextApprovers.Any())
            {
                await _context.EpeTransactionheaders
                    .Where(h => h.Transactionid == transactionId)
                    .ExecuteUpdateAsync(b => b.SetProperty(h => h.Status, "P"));
            }
            else
            {
                await _context.EpeTransactionheaders
                    .Where(h => h.Transactionid == transactionId)
                    .ExecuteUpdateAsync(b => b
                        .SetProperty(h => h.Status, "C")
                        .SetProperty(h => h.Finaldecision, "Approved")
                        .SetProperty(h => h.Modifieddate, DateTime.Now));
            }
        }
        else // Disapproved
        {
            var targetLineSequence = await _context.EpeTransactionapprovers
                .Where(x => x.Transapproverid == transApproverId)
                .Select(x => x.Linesequence)
                .FirstOrDefaultAsync();

            await _context.EpeTransactionapprovers
                .Where(a => a.Transactionid == transactionId
                         && a.Linesequence == targetLineSequence
                         && a.Respond == 0)
                .ExecuteUpdateAsync(b => b
                    .SetProperty(a => a.Respond, 2)
                    .SetProperty(a => a.Responddate, DateTime.Now)
                    .SetProperty(a => a.Remarks, "Auto-disapproved via email"));

            await _context.EpeTransactionheaders
                .Where(h => h.Transactionid == transactionId)
                .ExecuteUpdateAsync(b => b
                    .SetProperty(h => h.Status, "C")
                    .SetProperty(h => h.Finaldecision, "Disapproved")
                    .SetProperty(h => h.Modifieddate, DateTime.Now));
        }
    }
}
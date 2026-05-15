// Controllers/EmailApprovalController.cs
using EPEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Entity.Infrastructure.Interception;

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
        var baseUrl = "https://apps.fastlogistics.com.ph/epe2dev"; // TODO: Move to config
        //var baseUrl = "http://localhost:3000/epe2dev"; // For development, change to production URL when deploying

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
                .FirstOrDefaultAsync(t => t.Transactionid == record.TransactionId);

            if (transaction == null)
                return Redirect($"{baseUrl}/#/email-response?status=not_found");

            if (transaction.Status == "C")
                return Redirect($"{baseUrl}/#/email-response?status=already_closed&transactionId={record.TransactionId}");

            // Get the specific approver record
            var approver = await _context.EpeTransactionapprovers
                .FirstOrDefaultAsync(a => a.Transactionid == transaction.Transactionid && a.Transapproverid == record.TransApproverId && a.Isdelete == 0);

            if (approver == null)
                return Redirect($"{baseUrl}/#/email-response?status=invalid");

            if (approver.Respond > 0)
                return Redirect($"{baseUrl}/#/email-response?status=already_responded&transactionId={record.TransactionId}");

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var respond = record.Action == "approve" ? 1 : 2;



                // Process the approval (reuse same logic as ExemptionController.RespondToApproval)
                await ProcessApproval(record.TransactionId, record.TransApproverId, respond, "Approved via email");

                // Mark token as used
                record.IsUsed = true;

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

        if (requester == null)
        {
            _logger.LogWarning("No requester found for transaction {TransactionId}", transactionId);
            return;
        }

        _logger.LogInformation("Processing approval for transaction {TransactionId} by requester {Requester}", transactionId, requester);

        var requesterDetails = await _context.CoreVEmployeeDetails
            .FirstOrDefaultAsync(e => e.EmplId == requester);

        if (requesterDetails != null)
        {
            _logger.LogInformation("Requester: {Name} - {Email}", requesterDetails.Employeename, requesterDetails.Emailadd);
        }

        // Update current approver
        var approvers = await _context.EpeTransactionapprovers
            .Where(a => a.Transapproverid == transApproverId)
            .ToListAsync();

        foreach (var a in approvers)
        {
            a.Respond = respond;
            a.Responddate = DateTime.Now;
            a.Remarks = remarks;
        }

        //await _context.SaveChangesAsync();

        if (respond == 1)
            await HandleApproval(transactionId, transApproverId);
        else
            await HandleDisapproval(transactionId, transApproverId);


    }

    private async Task HandleApproval(long transactionId, long transApproverId)
    {
        //IDbContextTransaction? transaction = null;

        try
        {

            //_logger.LogInformation("Before transaction");

            //var transaction = await _context.Database.BeginTransactionAsync();

            //_logger.LogInformation("Transaction started");
            var allApprovers = await GetApprovers(transactionId);

            if (!allApprovers.Any())
            {
                _logger.LogWarning(
                    "No approvers found for transaction {TransactionId} during approval handling",
                    transactionId);

                return;
            }

            var current = allApprovers
                .FirstOrDefault(a => a.Transapproverid == transApproverId);

            if (current == null)
                return;

            _logger.LogInformation(
                "Current approver line sequence: {LineSequence} and sort: {Sort} for transaction {TransactionId}",
                current.Linesequence,
                current.Sort,
                transactionId);

            var groupIds = allApprovers
                .Where(a => a.Sort == current.Sort && a.Respond == 0)
                .Select(a => a.Transapproverid)
                .ToList();

            _logger.LogInformation(
                "Found {Count} other approvers in the same group to auto-approve for transaction {TransactionId}",
                groupIds.Count,
                transactionId);

            if (groupIds.Any())
            {
                var approversToUpdate = await _context.EpeTransactionapprovers
                    .Where(a => groupIds.Contains(a.Transapproverid))
                    .ToListAsync();

                foreach (var approver in approversToUpdate)
                {
                    approver.Respond = 1;
                    approver.Responddate = DateTime.Now;
                    approver.Remarks = "Approved via group approval";
                }
            }

            var updated = await GetApprovers(transactionId);

            var hasNext = updated.Any(a =>
                a.Linesequence > current.Linesequence &&
                a.Respond == 0);

            var transHeader = await _context.EpeTransactionheaders
                .FirstOrDefaultAsync(h => h.Transactionid == transactionId);

            if (transHeader != null)
            {
                transHeader.Status = hasNext ? "P" : "C";
                transHeader.Finaldecision = hasNext ? null : "Approved";
                transHeader.Modifieddate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            //await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            //await transaction.RollbackAsync();

            _logger.LogError(
                e,
                "Error handling approval for transaction {TransactionId} and approver {TransApproverId}",
                transactionId,
                transApproverId);

            throw;
        }
    }

    //private async Task HandleApproval(long transactionId, long transApproverId)
    //{

    //    var transaction = await _context.Database.BeginTransactionAsync();
    //    try
    //    {
    //        var allApprovers = await GetApprovers(transactionId);

    //        if (!allApprovers.Any())
    //        {
    //            _logger.LogWarning("No approvers found for transaction {TransactionId} during approval handling", transactionId);
    //            return;
    //        }


    //        var current = allApprovers.FirstOrDefault(a => a.Transapproverid == transApproverId);
    //        if (current == null) return;

    //        _logger.LogInformation("Current approver line sequence: {LineSequence} and sort: {Sort} for transaction {TransactionId}", current.Linesequence, current.Sort, transactionId);

    //        var groupIds = allApprovers
    //        .Where(a => a.Sort == current.Sort && a.Respond == 0)
    //        .Select(a => a.Transapproverid)
    //        .ToList();

    //        _logger.LogInformation("Found {Count} other approvers in the same group to auto-approve for transaction {TransactionId}", groupIds.Count, transactionId);

    //        if (groupIds.Any())
    //        {

    //            var approversToUpdate = await _context.EpeTransactionapprovers
    //                .Where(a => groupIds.Contains(a.Transapproverid))
    //                .ToListAsync();

    //            foreach (var approver in approversToUpdate)
    //            {
    //                approver.Respond = 1;
    //                approver.Responddate = DateTime.Now;
    //                approver.Remarks = "Approved via group approval";
    //            }
    //        }

    //        var updated = await GetApprovers(transactionId);

    //        var hasNext = updated.Any(a =>
    //            a.Linesequence > current.Linesequence &&
    //            a.Respond == 0);

    //        var transHeader = await _context.EpeTransactionheaders
    //            .Where(h => h.Transactionid == transactionId)
    //            .FirstOrDefaultAsync();

    //        transHeader.Status = hasNext ? "P" : "C";
    //        transHeader.Finaldecision = hasNext ? null : "Approved";
    //        transHeader.Modifieddate = DateTime.Now;

    //    }
    //    catch (Exception e)
    //    {
    //        transaction.Rollback();
    //        _logger.LogError(e, "Error handling approval for transaction {TransactionId} and approver {TransApproverId}", transactionId, transApproverId);
    //        throw;
    //    }

    //}

    private async Task HandleDisapproval(long transactionId, long transApproverId)
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var lineSequence = await _context.EpeTransactionapprovers
            .Where(x => x.Transapproverid == transApproverId)
            .Select(x => x.Linesequence)
            .FirstOrDefaultAsync();

            var approver = await _context.EpeTransactionapprovers
                 .Where(a => a.Transactionid == transactionId
                          && a.Linesequence == lineSequence
                          && a.Respond == 0)
                 .FirstOrDefaultAsync();

            if (approver != null)
            {
                _logger.LogInformation("Disapproving approver with line sequence {LineSequence} for transaction {TransactionId}", lineSequence, transactionId);
            }
            else
            {
                _logger.LogWarning("No approver found to disapprove with line sequence {LineSequence} for transaction {TransactionId}", lineSequence, transactionId);
            }

            approver.Respond = 2;
            approver.Responddate = DateTime.Now;
            approver.Remarks = "Disapproved via email";         

            var transHeader = await _context.EpeTransactionheaders
                .Where(h => h.Transactionid == transactionId)
                .FirstOrDefaultAsync();

            transHeader.Status = "C";
            transHeader.Finaldecision = "Disapproved";
            transHeader.Modifieddate = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

        }
        catch (Exception e)
        {
            transaction.Rollback();
            _logger.LogError(e, "Failed to Disapprove transaction {TransactionId} for approver {TransApproverId}", transactionId, transApproverId);
        }

    }

    private Task<List<EpeTransactionapprover>> GetApprovers(long transactionId)
    {
        return _context.EpeTransactionapprovers
            .Where(a => a.Transactionid == transactionId && a.Isdelete == 0)
            .OrderBy(a => a.Linesequence)
            .ThenBy(a => a.Sort)
            .ToListAsync();
    }

    //private async Task ProcessApproval(long transactionId, long transApproverId, int respond, string remarks)
    //{
    //    var requester = await _context.EpeTransactionheaders
    //        .Where(t => t.Transactionid == transactionId)
    //        .Select(t => t.Requestorid)
    //        .FirstOrDefaultAsync();

    //    if (requester != null)
    //    {
    //        _logger.LogInformation("Processing email approval for transaction {TransactionId} by requester {Requester}", transactionId, requester);
    //    }
    //    else
    //    {
    //        _logger.LogWarning("No requester found for transaction {TransactionId} during email approval processing", transactionId);

    //        var requesterDetails = await _context.CoreVEmployeeDetails
    //        .FirstOrDefaultAsync(e => e.EmplId == requester);

    //        if (requesterDetails != null)
    //        {
    //            _logger.LogInformation("Requester details - Name: {Name}, Email: {Email}", requesterDetails.Employeename, requesterDetails.Emailadd);
    //        }
    //        else
    //        {
    //            _logger.LogWarning("No employee details found for requester {Requester} during email approval processing", requester);
    //        }

    //        //var transacApp =await _context.EpeTransactionapprovers
    //        //.Where(a => a.Transapproverid == transApproverId)
    //        //.ExecuteUpdateAsync(b => b
    //        //    .SetProperty(a => a.Respond, respond)
    //        //    .SetProperty(a => a.Responddate, DateTime.Now)
    //        //    .SetProperty(a => a.Remarks, remarks));

    //        var approvers = await _context.EpeTransactionapprovers
    //            .Where(a => a.Transapproverid == transApproverId)
    //            .ToListAsync();

    //        foreach (var approver in approvers)
    //        {
    //            approver.Respond = respond;
    //            approver.Responddate = DateTime.Now;
    //            approver.Remarks = remarks;
    //        }

    //        await _context.SaveChangesAsync();

    //        if (respond == 1) // Approved
    //        {
    //            var allApprovers = await _context.EpeTransactionapprovers
    //                .Where(a => a.Transactionid == transactionId && a.Isdelete == 0)
    //                .OrderBy(a => a.Linesequence).ThenBy(a => a.Sort)
    //                .ToListAsync();

    //            if (!allApprovers.Any())
    //            {
    //                _logger.LogWarning("No approvers found for transaction {TransactionId} during email approval processing", transactionId);
    //                return;
    //            }

    //            var currentApprover = allApprovers.First(a => a.Transapproverid == transApproverId);

    //            if (currentApprover == null)
    //            {
    //                _logger.LogWarning("Current approver with ID {TransApproverId} not found for transaction {TransactionId} during email approval processing", transApproverId, transactionId);
    //                return;
    //            }

    //            // Auto-approve others in same sort group
    //            var groupIds = allApprovers
    //            .Where(a => a.Sort == currentApprover.Sort && a.Respond == 0)
    //            .Select(a => a.Transapproverid).ToList();

    //            if (groupIds.Any())
    //            {
    //                _logger.LogInformation("Auto-approving {Count} other approvers in the same group for transaction {TransactionId}", groupIds.Count, transactionId);
    //            }
    //            else
    //            {
    //                _logger.LogInformation("No other approvers to auto-approve in the same group for transaction {TransactionId}", transactionId);
    //            }

    //            if (groupIds.Any())
    //            {
    //                await _context.EpeTransactionapprovers
    //                    .Where(a => groupIds.Contains(a.Transapproverid))
    //                    .ExecuteUpdateAsync(b => b
    //                        .SetProperty(a => a.Respond, 1)
    //                        .SetProperty(a => a.Responddate, DateTime.Now)
    //                        .SetProperty(a => a.Remarks, "Auto-approved: group member approved via email"));
    //            }

    //            // Re-fetch to check next sequence
    //            var updatedApprovers = await _context.EpeTransactionapprovers
    //                .Where(a => a.Transactionid == transactionId && a.Isdelete == 0)
    //                .OrderBy(a => a.Linesequence).ThenBy(a => a.Sort)
    //                .ToListAsync();

    //            if (!updatedApprovers.Any())
    //            {
    //                _logger.LogWarning("No approvers found after update for transaction {TransactionId} during email approval processing", transactionId);
    //                return;
    //            }

    //            var currentGroupSequences = allApprovers
    //            .Where(a => a.Sort == currentApprover.Sort)
    //            .Select(a => a.Linesequence).Distinct().ToHashSet();

    //            if (currentGroupSequences.Count == 0)
    //            {
    //                _logger.LogWarning("No group sequences found for current approver with ID {TransApproverId} for transaction {TransactionId} during email approval processing", transApproverId, transactionId);
    //            }

    //            var nextApprovers = updatedApprovers
    //            .Where(a => !currentGroupSequences.Contains(a.Linesequence)
    //                     && a.Linesequence > currentApprover.Linesequence
    //                     && a.Respond == 0)
    //            .ToList();

    //            if (nextApprovers.Any())
    //            {
    //                _logger.LogInformation("Next approvers found for transaction {TransactionId} after email approval processing: {Count}", transactionId, nextApprovers.Count);
    //            }
    //            else
    //            {
    //                _logger.LogInformation("No next approvers found for transaction {TransactionId} after email approval processing", transactionId);
    //            }

    //            if (nextApprovers.Any())
    //            {
    //                await _context.EpeTransactionheaders
    //                    .Where(h => h.Transactionid == transactionId).ExecuteUpdateAsync(b => b.SetProperty(h => h.Status, "P"));
    //            }
    //            else
    //            {
    //                await _context.EpeTransactionheaders
    //                    .Where(h => h.Transactionid == transactionId)
    //                    .ExecuteUpdateAsync(b => b
    //                        .SetProperty(h => h.Status, "C")
    //                        .SetProperty(h => h.Finaldecision, "Approved")
    //                        .SetProperty(h => h.Modifieddate, DateTime.Now));
    //            }
    //        }
    //        else // Disapproved
    //        {
    //            var targetLineSequence = await _context.EpeTransactionapprovers
    //                .Where(x => x.Transapproverid == transApproverId)
    //                .Select(x => x.Linesequence)
    //                .FirstOrDefaultAsync();

    //            if (targetLineSequence != null) {
    //                    _logger.LogInformation("Disapproving approver with line sequence {LineSequence} and all pending approvers in the same line for transaction {TransactionId}", targetLineSequence, transactionId);
    //                }
    //                else
    //                {
    //                    _logger.LogWarning("No line sequence found for approver with ID {TransApproverId} during disapproval processing for transaction {TransactionId}", transApproverId, transactionId);
    //            }


    //            await _context.EpeTransactionapprovers
    //                .Where(a => a.Transactionid == transactionId
    //                         && a.Linesequence == targetLineSequence
    //                         && a.Respond == 0)
    //                .ExecuteUpdateAsync(b => b
    //                    .SetProperty(a => a.Respond, 2)
    //                    .SetProperty(a => a.Responddate, DateTime.Now)
    //                    .SetProperty(a => a.Remarks, "Auto-disapproved via email"));

    //            await _context.EpeTransactionheaders
    //                .Where(h => h.Transactionid == transactionId)
    //                .ExecuteUpdateAsync(b => b
    //                    .SetProperty(h => h.Status, "C")
    //                    .SetProperty(h => h.Finaldecision, "Disapproved")
    //                    .SetProperty(h => h.Modifieddate, DateTime.Now));
    //        }
    //    }
    //}
}
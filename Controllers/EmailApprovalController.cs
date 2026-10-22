// Controllers/EmailApprovalController.cs
using EPEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EPEAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmailApprovalController : ControllerBase
{
    private readonly EpeDbContext _context;
    private readonly ILogger<EmailApprovalController> _logger;
    private readonly UrlBaseSettings _urlBaseSettings;

    public EmailApprovalController(EpeDbContext context, ILogger<EmailApprovalController> logger, IOptions<UrlBaseSettings> urlBaseSettings)
    {
        _context = context;
        _logger = logger;
        _urlBaseSettings = urlBaseSettings.Value;
    }

    // GET /api/emailapproval/preview?token=xxx
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string token)
    {
        var baseUrl = _urlBaseSettings.View;

        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { ok = false, error = "missing_token" });

            var record = await _context.EpeEmailApprovalTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (record == null)
            {
                if (IsAjaxRequest())
                    return NotFound(new { ok = false, error = "invalid" });
                return Redirect($"{baseUrl}/#/email-response?status=invalid&token={Uri.EscapeDataString(token)}");
            }

            //if (!IsAjaxRequest() && string.IsNullOrEmpty(Request.Query["confirm"]))
            //{
            //    return Redirect($"{baseUrl}/#/email-response?token={Uri.EscapeDataString(token)}&action={Uri.EscapeDataString(record.Action)}");
            //}

            if (record.IsUsed)
            {
                if (IsAjaxRequest())
                    return BadRequest(new { ok = false, error = "already_used", transactionId = record.TransactionId });
                return Redirect($"{baseUrl}/#/email-response?status=already_used&transactionId={record.TransactionId}&token={Uri.EscapeDataString(token)}");
            }

            if (record.ExpiresAt < DateTime.Now)
            {
                if (IsAjaxRequest())
                    return BadRequest(new { ok = false, error = "expired", transactionId = record.TransactionId });
                return Redirect($"{baseUrl}/#/email-response?status=expired&transactionId={record.TransactionId}&token={Uri.EscapeDataString(token)}");
            }

            var transaction = await _context.EpeTransactionheaders
                .FirstOrDefaultAsync(t => t.Transactionid == record.TransactionId);

            if (transaction == null)
            {
                if (IsAjaxRequest())
                    return NotFound(new { ok = false, error = "not_found" });
                return Redirect($"{baseUrl}/#/email-response?status=not_found&token={Uri.EscapeDataString(token)}");
            }

            if (transaction.Status == "C")
            {
                if (IsAjaxRequest())
                    return BadRequest(new { ok = false, error = "already_closed", transactionId = record.TransactionId });
                return Redirect($"{baseUrl}/#/email-response?status=already_closed&transactionId={record.TransactionId}&token={Uri.EscapeDataString(token)}");
            }

            // Get basic transaction preview info
            var preview = new
            {
                id = transaction.Transactionid,
                number = transaction.Transactionnumber,
                summary = transaction.Details,
                status = transaction.Status,
                requesterId = transaction.Requestorid,
            };

            var allowed = new List<string> { "approve", "disapprove" };

            // If opened directly from email in a browser (not an XHR/json request), redirect to the frontend preview page
            //if (!IsAjaxRequest())
            //{
                return Redirect($"{baseUrl}/#/email-response?token={Uri.EscapeDataString(token)}&action={Uri.EscapeDataString(record.Action)}");
            //}

            //return Ok(new
            //{
            //    ok = true,
            //    token = token,
            //    action = record.Action,       
            //    transaction = preview,
            //    allowedActions = allowed,
            //    expiresAt = record.ExpiresAt
            //});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview failed for token {Token}", token);
            return StatusCode(500, new { ok = false, error = "server_error" });
        }
    }

    // GET /api/emailapproval/act?token=xxx
    // This is what the email button hits. Returns redirect when opened directly from email; returns JSON when called via XHR/Accept:application/json
    [HttpGet("act")]
    public async Task<IActionResult> ActOnToken([FromQuery] string token)
    {
        var baseUrl = _urlBaseSettings.View; 

        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { ok = false, error = "missing_token" });

            var record = await _context.EpeEmailApprovalTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (record == null)
            {
                if (IsAjaxRequest())
                    return NotFound(new { ok = false, error = "invalid" });
                return Redirect($"{baseUrl}/#/email-response?status=invalid&token={Uri.EscapeDataString(token)}");
            }

            if (!IsAjaxRequest() && string.IsNullOrEmpty(Request.Query["confirm"]))
            {
                return Redirect($"{baseUrl}/#/email-response?token={Uri.EscapeDataString(token)}&action={Uri.EscapeDataString(record.Action)}");
            }

            if (record.IsUsed)
            {
                if (IsAjaxRequest())
                    return BadRequest(new { ok = false, error = "already_used", transactionId = record.TransactionId });
                return Redirect($"{baseUrl}/#/email-response?status=already_used&transactionId={record.TransactionId}&token={Uri.EscapeDataString(token)}");
            }

            if (record.ExpiresAt < DateTime.Now)
            {
                if (IsAjaxRequest())
                    return BadRequest(new { ok = false, error = "expired", transactionId = record.TransactionId });
                return Redirect($"{baseUrl}/#/email-response?status=expired&transactionId={record.TransactionId}&token={Uri.EscapeDataString(token)}");
            }

            var transaction = await _context.EpeTransactionheaders
                .FirstOrDefaultAsync(t => t.Transactionid == record.TransactionId);

            if (transaction == null)
            {
                if (IsAjaxRequest())
                    return NotFound(new { ok = false, error = "not_found" });
                return Redirect($"{baseUrl}/#/email-response?status=not_found&token={Uri.EscapeDataString(token)}");
            }

            if (transaction.Status == "C")
            {
                if (IsAjaxRequest())
                    return BadRequest(new { ok = false, error = "already_closed", transactionId = record.TransactionId });
                return Redirect($"{baseUrl}/#/email-response?status=already_closed&transactionId={record.TransactionId}&token={Uri.EscapeDataString(token)}");
            }

            // Get the specific approver record
            var approver = await _context.EpeTransactionapprovers
                .FirstOrDefaultAsync(a => a.Transactionid == transaction.Transactionid && a.Transapproverid == record.TransApproverId && a.Isdelete == 0);

            if (approver == null)
            {
                if (IsAjaxRequest())
                    return NotFound(new { ok = false, error = "invalid" });
                return Redirect($"{baseUrl}/#/email-response?status=invalid&token={Uri.EscapeDataString(token)}");
            }

            if (approver.Respond > 0)
            {
                if (IsAjaxRequest())
                    return BadRequest(new { ok = false, error = "already_responded", transactionId = record.TransactionId });
                return Redirect($"{baseUrl}/#/email-response?status=already_responded&transactionId={record.TransactionId}&token={Uri.EscapeDataString(token)}");
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var respond = record.Action == "approve" ? 1 : 2;

                // Process the approval (reuse same logic as ExemptionController.RespondToApproval)
                await ProcessApproval(record.TransactionId, record.TransApproverId, respond, record.Action == "approve" ? "Approved via email" : "Disapproved via email");

                // Mark token as used
                record.IsUsed = true;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                var action = record.Action == "approve" ? "approved" : "disapproved";

                if (IsAjaxRequest())
                {
                    return Ok(new { ok = true, result = action, message = $"Transaction {action}", transactionId = record.TransactionId });
                }

                return Redirect($"{baseUrl}/#/email-response?status=success&action={action}&transactionId={record.TransactionId}&token={Uri.EscapeDataString(token)}");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Failed processing token {Token}", token);
                if (IsAjaxRequest())
                    return StatusCode(500, new { ok = false, error = "processing_failed", transactionId = record.TransactionId });
                return Redirect($"{baseUrl}/#/email-response?status=error&transactionId={record.TransactionId}&token={Uri.EscapeDataString(token)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email approval token processing failed for token {Token}", token);
            if (IsAjaxRequest())
                return StatusCode(500, new { ok = false, error = "server_error" });
            return Redirect($"{baseUrl}/#/email-response?status=error&token={Uri.EscapeDataString(token)}");
        }
    }

    private bool IsAjaxRequest()
    {
        var x = Request.Headers["X-Requested-With"].ToString();
        var accept = Request.Headers["Accept"].ToString();
        return (!string.IsNullOrEmpty(x) && x.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
               || (!string.IsNullOrEmpty(accept) && accept.Contains("application/json"));
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

        if (respond == 1)
            await HandleApproval(transactionId, transApproverId);
        else
            await HandleDisapproval(transactionId, transApproverId);

    }

    private async Task HandleApproval(long transactionId, long transApproverId)
    {
        try
        {
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

            var updated = await GetApprovers(transactionId);

            var currentSort = current.Sort;

            var nextSortGroup = updated
                .Where(a => a.Respond == 0)
                .GroupBy(a => a.Sort ?? 0)
                .Where(g => g.Key > (currentSort ?? 0))
                .OrderBy(g => g.Key)
                .FirstOrDefault();

            var transHeader = await _context.EpeTransactionheaders
                .FirstOrDefaultAsync(h => h.Transactionid == transactionId);

            if (transHeader != null)
            {
                transHeader.Status = nextSortGroup != null ? "P" : "C";
                transHeader.Finaldecision = nextSortGroup != null ? null : "Approved";
                transHeader.Modifieddate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Error handling approval for transaction {TransactionId} and approver {TransApproverId}",
                transactionId,
                transApproverId);

            throw;
        }
    }

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

            // Check if current approver is MAIN (Linesequence == 1)
            var isMainApprover = approver != null && approver.Linesequence == 1;

            if (isMainApprover)
            {
                // Main approver disapproved - hide proxies from UI by soft-deleting them
                var proxyApprovers = await _context.EpeTransactionapprovers
                    .Where(a => a.Transactionid == transactionId
                             && a.Sort == approver.Sort
                             && a.Linesequence != 1
                             && a.Isdelete == 0)
                    .ToListAsync();

                foreach (var proxy in proxyApprovers)
                {
                    proxy.Isdelete = 1; // Hide from UI
                }
            }

            // Only the acting approver gets Respond=2 with their remarks
            // NO auto-disapproval for others
            if (approver != null)
            {
                approver.Respond = 2;
                approver.Responddate = DateTime.Now;
                approver.Remarks = "Disapproved via email";
            }

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
}

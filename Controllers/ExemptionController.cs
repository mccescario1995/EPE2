using EPEAPI.Models;
using EPEAPI.Services;
using EPEAPI.Dto;
using LinqKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExceptionController : ControllerBase
    {
        private readonly EpeDbContext _context;
        private readonly EmailNotificationService _emailService;
        private readonly ILogger<ExceptionController> _logger;

        public ExceptionController(EpeDbContext contextdb, EmailNotificationService emailService, ILogger<ExceptionController> logger)
        {
            _context = contextdb;
            _emailService = emailService;
            _logger = logger;
        }

        // Helper method to get the current user ID from authentication
        private long GetCurrentUserId()
        {
            // First try to get from session (used by some controllers)
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue)
            {
                return sessionUserId.Value;
            }

            // Try to get from token header (used by TokenAuthenticationFilter)
            if (HttpContext.Request.Headers.ContainsKey("token"))
            {
                var token = HttpContext.Request.Headers["token"].ToString();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                    if (jsonToken != null)
                    {
                        var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                        if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                        {
                            return userId;
                        }
                    }
                }
                catch
                {
                    // If JWT parsing fails, try the custom token format used by TokenManager
                    try
                    {
                        var fromBase64 = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                        var credentials = fromBase64.Split(':');
                        if (credentials.Length >= 3 && credentials[1].EndsWith("tsaf"))
                        {
                            if (long.TryParse(credentials[0], out var userId))
                            {
                                return userId;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore and continue to return 0
                    }
                }
            }

            // Try to get from Authorization header (Bearer token)
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                        if (jsonToken != null)
                        {
                            var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                            {
                                return userId;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore and continue to return 0
                    }
                }
            }

            // Return 0 as fallback (should be improved in a real application)
            return 0;
        }

        [HttpPost]
        public async Task<IActionResult> GetExcemptionList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null
            )
        {
            try
            {

                var query = _context.EpeVTransactionHeaders.Where(u => u.IsDelete == 0);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchWords = search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // Start with TRUE so we can AND each word
                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word) ||
                            u.TransactionNumber.ToString().ToLower().Contains(word) ||
                            u.RequesterName.ToString().Contains(word) ||
                            u.FinalDecision.ToString().Contains(word) ||
                            u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    items = items,

                });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }

        }

        [HttpPost("pending")]
        public async Task<IActionResult> GetExcemptionPendingList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null
            )
        {
            try
            {

                var query = _context.EpeVTransactionHeaders.Where(u => u.IsDelete == 0);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchWords = search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // Start with TRUE so we can AND each word
                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word) ||
                            u.TransactionNumber.ToString().ToLower().Contains(word) ||
                            u.RequesterName.ToString().Contains(word) ||
                            u.FinalDecision.ToString().Contains(word) ||
                            u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    items = items,

                });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }

        }

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> EditExcemptionListId(int id)
        {
            try
            {
                var item = await _context.EpeVTransactionHeaders
                    .Where(u => u.TransactionId == id && u.IsDelete == 0)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return NotFound(new { success = false, message = "Exemption request not found" });
                }
                var imHead = await _context.CoreVEmployeeDetails
                                    .Where(e => e.EmplId == item.RequesterId)
                                    .FirstOrDefaultAsync();

                var approvers = await _context.EpeVTransactionApprovers
                    .Where(a => a.TransactionId == id)
                    .OrderBy(a => a.LineSequence)
                    .ThenBy(a => a.Sort)
                    .ToListAsync();

                var approversWithRole = approvers.Select(a =>
                {

                    var approverName = a.ApproverName;
                    var approverEmail = a.ApproverEmail;
                    var employeeId = a.EmployeeId;

                    if (a.ApproverType == "IM" && imHead != null)
                    {
                        approverName = imHead.ImmediateName;
                        approverEmail = imHead.ImmediateEmail;
                        employeeId = imHead.ImmediateId;
                    }

                    return new
                    {
                        a.TransApproverId,
                        a.TransactionId,
                        a.PolicyApproverId,
                        EmployeeId = employeeId,
                        ApproverName = approverName,
                        ApproverEmail = approverEmail,
                        a.ApproverType,
                        a.LineSequence,
                        a.Sort,
                        a.Remarks,
                        a.Respond,
                        a.RespondDate,
                        a.IsSent,
                        a.SentDate,
                        a.SentCount,
                        a.LastSendDate,
                        a.IsDelete,
                        a.CreateDate,
                        IsMainApprover = a.LineSequence == 1,
                        ApproverRole = a.LineSequence == 1 ? "Main" : "Proxy"
                    };
                }).ToList();

                if (item == null)
                {
                    return NotFound(new { success = false, message = "Exemption request not found" });
                }
                return Ok(new { success = true, item, approvers = approversWithRole });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }

        }

        [HttpPost("view/{id}")]
        public async Task<IActionResult> ViewExcemptionById(int id)
        {
            try
            {
                var item = await _context.EpeVTransactionHeaders
                    .Where(u => u.TransactionId == id && u.IsDelete == 0)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return NotFound(new { success = false, message = "Exemption request not found" });
                }

                var imHead = await _context.CoreVEmployeeDetails
                    .Where(e => e.EmplId == item.RequesterId)
                    .FirstOrDefaultAsync();

                var approvers = await _context.EpeVTransactionApprovers
                    .Where(a => a.TransactionId == id)
                    .OrderBy(a => a.LineSequence)
                    .ThenBy(a => a.Sort)
                    .ToListAsync();

                var approversWithRole = approvers.Select(a =>
                {

                    var approverName = a.ApproverName;
                    var approverEmail = a.ApproverEmail;
                    var employeeId = a.EmployeeId;

                    if (a.ApproverType == "IM" && imHead != null)
                    {
                        approverName = imHead.ImmediateName;
                        approverEmail = imHead.ImmediateEmail;
                        employeeId = imHead.ImmediateId;
                    }

                    return new
                    {
                        a.TransApproverId,
                        a.TransactionId,
                        a.PolicyApproverId,
                        EmployeeId = employeeId,
                        ApproverName = approverName,
                        ApproverEmail = approverEmail,
                        a.ApproverType,
                        a.LineSequence,
                        a.Sort,
                        a.Remarks,
                        a.Respond,
                        a.RespondDate,
                        a.IsSent,
                        a.SentDate,
                        a.SentCount,
                        a.LastSendDate,
                        a.IsDelete,
                        a.CreateDate,
                        IsMainApprover = a.LineSequence == 1,
                        ApproverRole = a.LineSequence == 1 ? "Main" : "Proxy"
                    };
                }).ToList();

                if (item == null)
                {
                    return NotFound(new { success = false, message = "Exemption request not found" });
                }
                return Ok(new { success = true, item, approvers = approversWithRole });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }

        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateExemptionRequest([FromBody] EpeTransactionheader request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var createdBy = request.Createdby;
                var requesterId = request.Requestorid;

                var userDetails = await _context.CoreVEmployeeDetails.Where(e => e.EmplId == requesterId).FirstOrDefaultAsync();

                request.Corporate = userDetails?.Corporate;
                request.Requestorid = requesterId;
                request.Finaldecision = "Pending";
                request.Createdby = createdBy.HasValue ? createdBy.Value : 0;
                request.Createddate = DateTime.UtcNow;

                // Client IP from headers
                var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                                ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "Unknown";

                // MAC address — server's own NIC (not the client's)
                var macAddress = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n =>
                        n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    ?.GetPhysicalAddress()
                    .ToString() ?? "Unknown";

                request.Ipaddress = ipAddress;
                request.Macaddress = macAddress;

                _context.EpeTransactionheaders.Add(request);

                await _context.SaveChangesAsync();

                // Get policy approvers and create transaction approvers
                var policyApprovers = await _context.EpePolicyapprovers
                    .Where(p => p.Policyid == request.Policyid && p.Isdelete == 0)
                    .OrderBy(p => p.Linesequence)
                    .ThenBy(p => p.Sort)
                    .ToListAsync();

                if (policyApprovers.Any())
                {
                    foreach (var policyApprover in policyApprovers)
                    {
                        var transApprover = new EpeTransactionapprover
                        {
                            Transactionid = request.Transactionid,
                            Approverid = policyApprover.Id.ToString(),
                            Approvertype = policyApprover.Approvertype,
                            Linesequence = policyApprover.Linesequence,
                            Sort = policyApprover.Sort,
                            Respond = 0,
                            Issent = 0,
                            Sentcount = 0,
                            Isdelete = 0,
                            Createddate = DateTime.UtcNow
                        };
                        _context.EpeTransactionapprovers.Add(transApprover);
                    }

                    await _context.SaveChangesAsync();

                    // Send email to first approver(s)
                    var firstSequence = policyApprovers.Min(p => p.Linesequence);
                    var firstApprovers = policyApprovers.Where(p => p.Linesequence == firstSequence).ToList();

                    foreach (var approver in firstApprovers)
                    {
                        string approverEmail = approver.Approveremail ?? "";
                        string approverName = approver.Approvername ?? "";

                        // If approver type is IM (Immediate Manager), get the requester's immediate head
                        if (approver.Approvertype == "IM" && userDetails != null)
                        {
                            approverEmail = userDetails.ImmediateEmail ?? "";
                            approverName = userDetails.ImmediateName ?? "";
                        }

                        if (!string.IsNullOrEmpty(approverEmail))
                        {
                            try
                            {
                                await _emailService.SendApprovalRequestEmailAsync(
                                    approverEmail,
                                    request.Transactionid,
                                    request.Transactionnumber,
                                    userDetails?.Employeename ?? requesterId,
                                    request.Details ?? "No details provided",
                                    int.Parse(userDetails?.EmplId)
                                );
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't fail the request creation
                                _logger.LogError(ex, "Failed to send approval request email to {Email}", approverEmail);
                            }
                        }
                    }

                    var requester = await _context.CoreVEmployeeDetails
                        .Where(r => r.EmplId == request.Requestorid)
                        .FirstOrDefaultAsync();

                    await _emailService.SendRequestCreatedEmailAsync(
                            requester.Emailadd,
                            request.Transactionnumber,
                            request.Details ?? "No details provided"
                        );
                }

                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Exemption request created successfully", item = request });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error" + e.Message);
            }

        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateExemptionRequest(int id, [FromBody] EpeTransactionheader request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingItem = await _context.EpeTransactionheaders
                    .Where(u => u.Transactionid == id && u.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (existingItem == null)
                {
                    return NotFound(new { success = false, message = "Exemption request not found" });
                }

                // Only allow editing these fields: Corporate, PolicyId, Details
                // Preserve system fields: Status, FinalDecision, CreatedBy, CreatedDate
                existingItem.Corporate = request.Corporate;
                existingItem.Policyid = request.Policyid;
                existingItem.Details = request.Details;
                existingItem.Modifieddate = DateTime.UtcNow;
                existingItem.Modifiedby = request.Modifiedby;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Exemption request updated successfully", item = existingItem });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteExemptionRequest(int id, [FromBody] ApproverRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingItem = await _context.EpeTransactionheaders
                    .Where(u => u.Transactionid == id && u.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (existingItem == null)
                {
                    return NotFound(new { success = false, message = "Exemption request not found" });
                }

                // Soft delete - mark as deleted
                existingItem.Isdelete = 1;
                existingItem.Modifiedby = request.UserId;
                existingItem.Modifieddate = DateTime.UtcNow;

                var exsistingApprovals = await _context.EpeTransactionapprovers
                    .Where(a => a.Transactionid == id && a.Isdelete == 0)
                    .ExecuteUpdateAsync(b => b.SetProperty(a => a.Isdelete, 1));

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Exemption request deleted successfully" });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("respond")]
        public async Task<IActionResult> RespondToApproval([FromBody] ApproverResponseRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transactionHeader = await _context.EpeTransactionheaders
                    .Where(u => u.Transactionid == request.TransactionId && u.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (transactionHeader == null)
                    return NotFound(new { success = false, message = "Exemption request not found" });

                var approver = await _context.EpeTransactionapprovers
                    .Where(a => a.Transapproverid == request.TransApproverId && a.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (approver == null)
                    return NotFound(new { success = false, message = "Approver not found" });

                var requester = await _context.CoreVEmployeeDetails
                    .Where(e => e.EmplId == transactionHeader.Requestorid)
                    .FirstOrDefaultAsync();

                // ✅ Update approver directly — bypasses change tracker
                await _context.EpeTransactionapprovers
                    .Where(a => a.Transapproverid == request.TransApproverId)
                    .ExecuteUpdateAsync(b => b
                        .SetProperty(a => a.Respond, request.Respond)
                        .SetProperty(a => a.Responddate, DateTime.UtcNow)
                        .SetProperty(a => a.Remarks, request.Remarks)
                    );

                if (request.Respond == 1) // Approved
                {
                    // Reload all approvers fresh after the update above
                    var allApprovers = await _context.EpeTransactionapprovers
                        .Where(a => a.Transactionid == request.TransactionId && a.Isdelete == 0)
                        .OrderBy(a => a.Linesequence)
                        .ThenBy(a => a.Sort)
                        .ToListAsync();

                    var currentApprover = allApprovers.First(a => a.Transapproverid == request.TransApproverId);
                    var currentSequence = currentApprover.Linesequence;
                    var currentSort = currentApprover.Sort;

                    // Same Sort regardless of Linesequence = same group (main + proxy)
                    var approversInGroup = allApprovers
                        .Where(a => a.Sort == currentSort && a.Respond == 0)
                        .ToList();

                    if (approversInGroup.Any())
                    {
                        var groupIds = approversInGroup.Select(a => a.Transapproverid).ToList();
                        await _context.EpeTransactionapprovers
                            .Where(a => groupIds.Contains(a.Transapproverid))
                            .ExecuteUpdateAsync(b => b
                                .SetProperty(a => a.Respond, 1)
                                .SetProperty(a => a.Responddate, DateTime.UtcNow)
                                .SetProperty(a => a.Remarks, "Auto-approved: group member approved")
                            );
                    }

                    // ✅ Next sequence = any Linesequence that is NOT in the current group's sequences
                    //    Reload after auto-approve to get fresh state
                    var updatedApprovers = await _context.EpeTransactionapprovers
                        .Where(a => a.Transactionid == request.TransactionId && a.Isdelete == 0)
                        .OrderBy(a => a.Linesequence)
                        .ThenBy(a => a.Sort)
                        .ToListAsync();

                    // Sequences occupied by the current sort group
                    var currentGroupSequences = allApprovers
                        .Where(a => a.Sort == currentSort)
                        .Select(a => a.Linesequence)
                        .Distinct()
                        .ToHashSet();

                    // Next approvers = those whose Linesequence is NOT part of current group
                    // AND have a higher Linesequence than the current approver
                    var nextApprovers = updatedApprovers
                        .Where(a => !currentGroupSequences.Contains(a.Linesequence)
                                && a.Linesequence > currentSequence
                                && a.Respond == 0)
                        .ToList();

                    if (nextApprovers.Any())
                    {
                        // At least one approved, more sequences ahead → Status = 'P'
                        await _context.EpeTransactionheaders
                            .Where(h => h.Transactionid == request.TransactionId)
                            .ExecuteUpdateAsync(b => b
                                .SetProperty(h => h.Status, "P")
                            );

                        var nextSequence = nextApprovers.Min(a => a.Linesequence);
                        var approversToNotify = nextApprovers
                            .Where(a => a.Linesequence == nextSequence)
                            .ToList();

                        foreach (var nextApprover in approversToNotify)
                        {
                            string approverEmail = "";

                            if (nextApprover.Approvertype == "IM" && requester != null)
                            {
                                approverEmail = requester.ImmediateEmail ?? "";
                            }
                            else
                            {
                                var policyApprover = await _context.EpePolicyapprovers
                                    .Where(e => e.Id == int.Parse(nextApprover.Approverid))
                                    .FirstOrDefaultAsync();
                                approverEmail = policyApprover?.Approveremail ?? "";
                            }

                            if (!string.IsNullOrEmpty(approverEmail))
                            {
                                try
                                {
                                    await _emailService.SendApprovalRequestEmailAsync(
                                        approverEmail,
                                        transactionHeader.Transactionid,
                                        transactionHeader.Transactionnumber,
                                        requester?.Employeename ?? transactionHeader.Requestorid,
                                        transactionHeader.Details ?? "No details provided",
                                        int.Parse(requester?.EmplId)
                                    );
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to send next approver email to {Email}", approverEmail);
                                }
                            }
                        }
                    }
                    else
                    {
                        // No pending approvers outside this group → fully approved
                        await _context.EpeTransactionheaders
                            .Where(h => h.Transactionid == request.TransactionId)
                            .ExecuteUpdateAsync(b => b
                                .SetProperty(h => h.Status, "C")
                                .SetProperty(h => h.Finaldecision, "Approved")
                            );

                        if (requester != null && !string.IsNullOrEmpty(requester.Emailadd))
                        {
                            try
                            {
                                await _emailService.SendStatusChangedEmailAsync(
                                    requester.Emailadd,
                                    transactionHeader.Transactionid,
                                    transactionHeader.Transactionnumber,
                                    "Pending",
                                    "Approved",
                                    "System"
                                );
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send approval email to {Email}", requester.Emailadd);
                            }
                        }
                    }
                }
                else if (request.Respond == 2) // Disapproved
                {
                    // Auto-disapprove everyone else in same sequence
                    await _context.EpeTransactionapprovers
                        .Where(a => a.Transactionid == request.TransactionId
                                 && a.Linesequence == approver.Linesequence
                                 && a.Respond == 0
                                 && a.Isdelete == 0)
                        .ExecuteUpdateAsync(b => b
                            .SetProperty(a => a.Respond, 2)
                            .SetProperty(a => a.Responddate, DateTime.UtcNow)
                            .SetProperty(a => a.Remarks, "Auto-disapproved: sequence disapproved")
                        );

                    // ✅ Any disapproval closes immediately
                    await _context.EpeTransactionheaders
                        .Where(h => h.Transactionid == request.TransactionId)
                        .ExecuteUpdateAsync(b => b
                            .SetProperty(h => h.Status, "C")
                            .SetProperty(h => h.Finaldecision, "Disapproved")
                        );

                    if (requester != null && !string.IsNullOrEmpty(requester.Emailadd))
                    {
                        try
                        {
                            await _emailService.SendStatusChangedEmailAsync(
                                requester.Emailadd,
                                transactionHeader.Transactionid,
                                transactionHeader.Transactionnumber,
                                "Pending",
                                "Disapproved",
                                "System"
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send disapproved email to {Email}", requester.Emailadd);
                        }
                    }
                }

                await transaction.CommitAsync();
                return Ok(new { success = true, message = "Response recorded successfully" });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error: " + e.Message);
            }
        }
        [HttpPost("resend/{id}")]
        public async Task<IActionResult> ResendApprovalRequestEmail(int id, [FromBody] ResendRequest request)
        {
            try
            {
                // Get the approver record
                var approver = await _context.EpeTransactionapprovers
                    .Where(a => a.Transapproverid == request.TransApproverId && a.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (approver == null)
                {
                    return NotFound(new { success = false, message = "Approver not found" });
                }

                // Get the transaction header
                var transactionHeader = await _context.EpeTransactionheaders
                    .Where(u => u.Transactionid == approver.Transactionid && u.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (transactionHeader == null)
                {
                    return NotFound(new { success = false, message = "Exemption request not found" });
                }

                // Get requester details
                var requester = await _context.CoreVEmployeeDetails
                    .Where(e => e.EmplId == transactionHeader.Requestorid)
                    .FirstOrDefaultAsync();

                // Determine approver email and name
                string approverEmail = "";
                string approverName = "";

                if (approver.Approvertype == "IM" && requester != null)
                {
                    // For IM type, get the requester's immediate head
                    approverEmail = requester.ImmediateEmail ?? "";
                    approverName = requester.ImmediateName ?? "";
                }
                else
                {
                    // Get from employee details using Approverid
                    var empDetail = await _context.EpePolicyapprovers
                        .Where(e => e.Id == int.Parse(approver.Approverid))
                        .FirstOrDefaultAsync();

                    approverEmail = empDetail?.Approveremail ?? "";
                    approverName = empDetail?.Approvername ?? "";
                }

                if (string.IsNullOrEmpty(approverEmail))
                {
                    return BadRequest(new { success = false, message = "Approver email not found" });
                }

                // Send the approval request email
                try
                {
                    await _emailService.SendApprovalRequestEmailAsync(
                        approverEmail,
                        transactionHeader.Transactionid,
                        transactionHeader.Transactionnumber,
                        transactionHeader.Requestorid,
                        transactionHeader.Details ?? "No details provided",
                        int.Parse(requester?.EmplId)
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resend approval request email to {Email}", approverEmail);
                    return StatusCode(500, new { success = false, message = "Failed to send email: " + ex.Message });
                }

                // Update tracking fields
                approver.Issent = 1;
                approver.Sentcount = approver.Sentcount + 1;
                approver.Lastsentdate = DateTime.UtcNow;

                // Set SentDate only on first send
                if (approver.Sentdate == null)
                {
                    approver.Sentdate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Approval request email resent successfully" });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }
    }
}
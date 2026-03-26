using EPEAPI.Models;
using EPEAPI.Services;
using EPEAPI.Dto;
using LinqKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

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
                                    request.Details ?? "No details provided"
                                );
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't fail the request creation
                                _logger.LogError(ex, "Failed to send approval request email to {Email}", approverEmail);
                            }
                        }
                    }
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
                // Get the transaction header
                var transactionHeader = await _context.EpeTransactionheaders
                    .Where(u => u.Transactionid == request.TransactionId && u.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (transactionHeader == null)
                {
                    return NotFound(new { success = false, message = "Exemption request not found" });
                }

                // Get the current approver
                var approver = await _context.EpeTransactionapprovers
                    .Where(a => a.Transapproverid == request.TransApproverId && a.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (approver == null)
                {
                    return NotFound(new { success = false, message = "Approver not found" });
                }

                // Update approver response
                approver.Remarks = request.Remarks;
                approver.Respond = request.Respond;
                approver.Responddate = DateTime.UtcNow;

                // Get requester details for email
                var requester = await _context.CoreVEmployeeDetails
                    .Where(e => e.EmplId == transactionHeader.Requestorid)
                    .FirstOrDefaultAsync();

                await _context.SaveChangesAsync();

                // If approved, check if main approver or proxy already approved
                if (request.Respond == 1) // Approved
                {
                    _logger.LogInformation("Approver {ApproverId} approved transaction {TransactionId}. Checking for next approvers...",
                        request.TransApproverId, request.TransactionId);

                    // Get all approvers for this transaction
                    var allApprovers = await _context.EpeTransactionapprovers
                        .Where(a => a.Transactionid == request.TransactionId && a.Isdelete == 0)
                        .OrderBy(a => a.Linesequence)
                        .ThenBy(a => a.Sort)
                        .ToListAsync();

                    var currentApprover = approver;
                    var currentSequence = approver.Linesequence;
                    var currentSort = approver.Sort;

                    _logger.LogInformation("Current approver: Sequence={Sequence}, Sort={Sort}", currentSequence, currentSort);

                    // AUTO-APPROVE: Approvers with same Sort are a group (main + proxy)
                    // If one approves, all others with same Sort are auto-approved
                    var approversInGroup = allApprovers
                        .Where(a => a.Sort == currentSort && a.Linesequence == currentSequence)
                        .ToList();

                    // Also auto-approve proxy at next sequence with same Sort (if exists)
                    var proxyInNextSequence = allApprovers
                        .Where(a => a.Sort == currentSort && a.Linesequence == currentSequence + 1)
                        .ToList();

                    var allGroupsToAutoApprove = approversInGroup.Concat(proxyInNextSequence).ToList();

                    _logger.LogInformation("Approvers in group (Sort={Sort}, Seq={Sequence}): {Count}. Proxies at next seq: {ProxyCount}",
                        currentSort, currentSequence, approversInGroup.Count, proxyInNextSequence.Count);

                    // Auto-approve all in group and proxy
                    foreach (var approverToApprove in allGroupsToAutoApprove)
                    {
                        if (approverToApprove.Respond == 0)
                        {
                            _logger.LogInformation("Auto-approving approver {ApproverId} in same group/proxy", approverToApprove.Transapproverid);
                            approverToApprove.Respond = 1;
                            approverToApprove.Responddate = DateTime.UtcNow;
                            approverToApprove.Remarks = "Auto-approved: Approved by Approver " + currentApprover.Transapproverid;
                        }
                    }

                    // Check if this group has approved (including proxy)
                    var hasApprovedInGroup = allGroupsToAutoApprove.Any(a => a.Respond == 1);
                    _logger.LogInformation("Has approved in group: {HasApproved}", hasApprovedInGroup);

                    await _context.SaveChangesAsync();

                    // Reload all approvers after auto-approval
                    allApprovers = await _context.EpeTransactionapprovers
                        .Where(a => a.Transactionid == request.TransactionId && a.Isdelete == 0)
                        .OrderBy(a => a.Linesequence)
                        .ThenBy(a => a.Sort)
                        .ToListAsync();

                    // Get next sequence approvers (linesequence > current sequence)
                    var nextApprovers = allApprovers.Where(a => a.Linesequence > currentSequence).ToList();
                    _logger.LogInformation("Next approvers count: {Count}", nextApprovers.Count);

                    if (nextApprovers.Any())
                    {
                        // If this group approved, send to next sequence (for that specific Sort group)
                        if (hasApprovedInGroup)
                        {
                            // At least one approved, still more approvers ahead → Status = 'P'
                            transactionHeader.Status = "P";

                            // Get the minimum sequence for next approvers (only those NOT auto-approved)
                            var nextSequence = nextApprovers.Min(a => a.Linesequence);
                            var approversToNotify = nextApprovers
                                .Where(a => a.Linesequence == nextSequence)
                                .ToList();

                            _logger.LogInformation("Notifying next sequence {Sequence}, count: {Count}",
                                nextSequence, approversToNotify.Count);

                            foreach (var nextApprover in approversToNotify)
                            {
                                string approverEmail = "";
                                string approverName = "";

                                // If approver type is IM (Immediate Manager), get the requester's immediate head
                                if (nextApprover.Approvertype == "IM" && requester != null)
                                {
                                    approverEmail = requester.ImmediateEmail ?? "";
                                    approverName = requester.ImmediateName ?? "";
                                }
                                else
                                {
                                    // Get from policy approvers using Approverid
                                    var approverDetail = await _context.EpePolicyapprovers
                                        .Where(e => e.Id == int.Parse(nextApprover.Approverid))
                                        .FirstOrDefaultAsync();

                                    // Get from employee details using Approverid
                                    //var empDetail = await _context.CoreVEmployeeDetails
                                    //    .Where(e => e.EmplId == nextApprover.Approverid)
                                    //    .FirstOrDefaultAsync();
                                    approverEmail = approverDetail?.Approveremail ?? "";
                                    approverName = approverDetail?.Approvername ?? "";
                                }

                                _logger.LogInformation("Preparing to send email to: {Email} ({Name})", approverEmail, approverName);

                                if (!string.IsNullOrEmpty(approverEmail))
                                {
                                    try
                                    {
                                        await _emailService.SendApprovalRequestEmailAsync(
                                            approverEmail,
                                            transactionHeader.Transactionid,
                                            transactionHeader.Transactionnumber,
                                            requester?.Employeename ?? transactionHeader.Requestorid,
                                            transactionHeader.Details ?? "No details provided"
                                        );

                                        _logger.LogInformation("Successfully sent email to next approver: {Email}", approverEmail);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to send next approver email to {Email}", approverEmail);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("No email found for next approver: {Name}, Type: {Type}",
                                        approverName, nextApprover.Approvertype);
                                }
                            }
                        }
                        // If not yet approved in group, wait for more responses in this group
                    }
                    else
                    {
                        // All sequences completed - update final decision and status
                        _logger.LogInformation("All sequences completed. Updating final decision to Approved and Status to C.");

                        transactionHeader.Finaldecision = "Approved";
                        transactionHeader.Status = "C";
                        await _context.SaveChangesAsync();

                        // Send final approval email to requester
                        if (requester != null && !string.IsNullOrEmpty(requester.Emailadd))
                        {
                            try
                            {
                                await _emailService.SendStatusChangedEmailAsync(
                                    requester.Emailadd,
                                    transactionHeader.Transactionnumber,
                                    "Pending",
                                    "Approved",
                                    "System"
                                );
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send final approval email to {Email}", requester.Emailadd);
                            }
                        }
                    }
                }
                else if (request.Respond == 2) // Disapproved
                {
                    // AUTO-DISAPPROVE: Also disapprove all others in the same sequence
                    var allApproversForDisapprove = await _context.EpeTransactionapprovers
                        .Where(a => a.Transactionid == request.TransactionId && a.Isdelete == 0)
                        .OrderBy(a => a.Linesequence)
                        .ThenBy(a => a.Sort)
                        .ToListAsync();

                    var currentSequence = approver.Linesequence;

                    // Disapprove all approvers in this sequence who haven't responded yet
                    var approversInSequence = allApproversForDisapprove
                        .Where(a => a.Linesequence == currentSequence)
                        .ToList();

                    foreach (var approverInSequence in approversInSequence)
                    {
                        if (approverInSequence.Respond == 0)
                        {
                            _logger.LogInformation("Auto-disapproving approver {ApproverId} in same sequence", approverInSequence.Transapproverid);
                            approverInSequence.Respond = 2;
                            approverInSequence.Responddate = DateTime.UtcNow;
                            approverInSequence.Remarks = "Auto-disapproved: Sequence disapproved by " + approver.Transapproverid;
                        }
                    }

                    // Update final decision to disapproved and mark as completed
                    _logger.LogInformation("Disapproved. Updating final decision to Disapproved and Status to C.");

                    transactionHeader.Finaldecision = "Disapproved";
                    transactionHeader.Status = "C";

                    await _context.SaveChangesAsync();

                    // Send disapproved email to requester
                    if (requester != null && !string.IsNullOrEmpty(requester.Emailadd))
                    {
                        try
                        {
                            await _emailService.SendStatusChangedEmailAsync(
                                requester.Emailadd,
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
                return StatusCode(500, "Error" + e.Message);
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
                        transactionHeader.Details ?? "No details provided"
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
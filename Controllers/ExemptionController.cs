using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using EPEAPI.Dto;
using EPEAPI.Models;
using EPEAPI.Services;
using LinqKit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//using LinqKit.Core

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExemptionController : ControllerBase
    {
        private readonly EpeDbContext _context;
        private readonly EmailNotificationService _emailService;
        private readonly FileUploadService _fileUploadService;
        private readonly ILogger<ExemptionController> _logger;
        private readonly IWebHostEnvironment _env;

        public ExemptionController(
            EpeDbContext contextdb,
            EmailNotificationService emailService,
            FileUploadService fileUploadService,
            ILogger<ExemptionController> logger,
            IWebHostEnvironment env
        )
        {
            _context = contextdb;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
            _logger = logger;
            _env = env;
        }

        public class ExemptionListRequest
        {
            public int UserId { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public string? Search { get; set; }
            public string? SortField { get; set; }
            public string? SortDirection { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public string? PolicyNames { get; set; }
        }

        [HttpPost("all")]
        public async Task<IActionResult> GetExcemptionList(
            [FromBody] ExemptionListRequest request,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var currentUserEmplId = request.UserId;
                var query = _context.EpeVTransactionHeaders.Where(u =>
                    u.FinalDecision == "Draft"
                    && u.IsDelete == 0
                    && u.RequesterId == currentUserEmplId.ToString()
                );

                var searchTerm = request.Search ?? search;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchWords = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word)
                            || u.TransactionNumber.ToString().ToLower().Contains(word)
                            || u.RequesterName.ToString().Contains(word)
                            || u.FinalDecision.ToString().Contains(word)
                            || u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                // Sorting
                query = request.SortField switch
                {
                    "transactionNumber" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.TransactionNumber)
                        : query.OrderBy(u => u.TransactionNumber),
                    "policyName" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PolicyName)
                        : query.OrderBy(u => u.PolicyName),
                    "createdDate" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.CreatedDate)
                        : query.OrderBy(u => u.CreatedDate),
                    _ => query.OrderByDescending(u => u.CreatedDate),
                };

                var totalCount = await query.CountAsync();

                var approvableTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId.ToString()
                        && a.Respond == 0
                        && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .ToListAsync();

                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;

                var items = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        TransactionId = h.TransactionId,
                        TransactionNumber = h.TransactionNumber,
                        PolicyName = h.PolicyName,
                        Status = h.Status,
                        RequesterName = h.RequesterName,
                        CreatedDate = h.CreatedDate,
                        RequesterId = h.RequesterId,
                        FinalDecision = h.FinalDecision,

                        CanApprove = approvableTransactionIds.Contains(h.TransactionId),
                    })
                    .ToListAsync();

                return Ok(
                    new
                    {
                        success = true,
                        items = items,
                        totalCount,
                        page,
                        pageSize,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("request-list")]
        public async Task<IActionResult> GetExcemptionRequestList(
            [FromBody] ExemptionListRequest request,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var currentUserEmplId = request.UserId.ToString();
                // 1) Transactions where the user is a pending approver
                var approvableTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId && a.Respond == 0 && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .Distinct()
                    .ToListAsync();

                // 2) Base query: pending, not deleted, AND
                //    (user is the requester OR user is a pending approver)
                var query = _context.EpeVTransactionHeaders.Where(u =>
                    u.IsDelete == 0
                    && u.FinalDecision != "Draft" || u.FinalDecision == "Approved" || u.FinalDecision == "Disapproved"
                    && (
                        u.RequesterId == currentUserEmplId
                        || approvableTransactionIds.Contains(u.TransactionId)
                    )
                );

                var searchTerm = request.Search ?? search;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchWords = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word)
                            || u.TransactionNumber.ToString().ToLower().Contains(word)
                            || u.RequesterName.ToString().Contains(word)
                            || u.FinalDecision.ToString().Contains(word)
                            || u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                // Sorting
                query = request.SortField switch
                {
                    "transactionNumber" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.TransactionNumber)
                        : query.OrderBy(u => u.TransactionNumber),
                    "policyName" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PolicyName)
                        : query.OrderBy(u => u.PolicyName),
                    "createdDate" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.CreatedDate)
                        : query.OrderBy(u => u.CreatedDate),
                    _ => query.OrderByDescending(u => u.CreatedDate),
                };

                if (!string.IsNullOrWhiteSpace(request.PolicyNames))
                {
                    var selectedIds = request
                        .PolicyNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => int.TryParse(p, out _))
                        .Select(int.Parse)
                        .ToList();

                    if (selectedIds.Any())
                    {
                        query = query.Where(u => selectedIds.Contains(u.PolicyId));
                    }
                }

                var totalCount = await query.CountAsync();

                approvableTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId.ToString()
                        && a.Respond == 0
                        && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .ToListAsync();

                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;

                var items = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        TransactionId = h.TransactionId,
                        TransactionNumber = h.TransactionNumber,
                        PolicyName = h.PolicyName,
                        Status = h.Status,
                        RequesterName = h.RequesterName,
                        CreatedDate = h.CreatedDate,
                        RequesterId = h.RequesterId,
                        FinalDecision = h.FinalDecision,

                        CanApprove = approvableTransactionIds.Contains(h.TransactionId),
                        HasApproverResponded = _context.EpeVTransactionApprovers.Any(a =>
                            a.TransactionId == h.TransactionId && a.Respond > 0
                        ),
                    })
                    .ToListAsync();

                return Ok(
                    new
                    {
                        success = true,
                        items = items,
                        totalCount,
                        page,
                        pageSize,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("draft")]
        public async Task<IActionResult> GetExcemptionDraftList(
            [FromBody] ExemptionListRequest request,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var currentUserEmplId = request.UserId;
                var query = _context.EpeVTransactionHeaders.Where(u =>
                    u.FinalDecision == "Draft"
                    && u.IsDelete == 0
                    && u.RequesterId == currentUserEmplId.ToString()
                );

                var searchTerm = request.Search ?? search;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchWords = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word)
                            || u.TransactionNumber.ToString().ToLower().Contains(word)
                            || u.RequesterName.ToString().Contains(word)
                            || u.FinalDecision.ToString().Contains(word)
                            || u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                // Sorting
                query = request.SortField switch
                {
                    "transactionNumber" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.TransactionNumber)
                        : query.OrderBy(u => u.TransactionNumber),
                    "policyName" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PolicyName)
                        : query.OrderBy(u => u.PolicyName),
                    "createdDate" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.CreatedDate)
                        : query.OrderBy(u => u.CreatedDate),
                    _ => query.OrderByDescending(u => u.CreatedDate),
                };

                var totalCount = await query.CountAsync();

                var approvableTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId.ToString()
                        && a.Respond == 0
                        && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .ToListAsync();

                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;

                var items = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        TransactionId = h.TransactionId,
                        TransactionNumber = h.TransactionNumber,
                        PolicyName = h.PolicyName,
                        Status = h.Status,
                        RequesterName = h.RequesterName,
                        CreatedDate = h.CreatedDate,
                        RequesterId = h.RequesterId,
                        FinalDecision = h.FinalDecision,

                        CanApprove = approvableTransactionIds.Contains(h.TransactionId),
                    })
                    .ToListAsync();

                return Ok(
                    new
                    {
                        success = true,
                        items = items,
                        totalCount,
                        page,
                        pageSize,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("pending")]
        public async Task<IActionResult> GetExcemptionPendingList(
            [FromBody] ExemptionListRequest request,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var currentUserEmplId = request.UserId.ToString();
                // 1) Transactions where the user is a pending approver
                var approvableTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId && a.Respond == 0 && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .Distinct()
                    .ToListAsync();

                // 2) Base query: pending, not deleted, AND
                //    (user is the requester OR user is a pending approver)
                var query = _context.EpeVTransactionHeaders.Where(u =>
                    u.FinalDecision == "Pending"
                    && u.IsDelete == 0
                    && approvableTransactionIds.Contains(u.TransactionId)
                );
                //&& (
                //        u.RequesterId == currentUserEmplId
                //        || approvableTransactionIds.Contains(u.TransactionId)
                //    )

                var searchTerm = request.Search ?? search;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchWords = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word)
                            || u.TransactionNumber.ToString().ToLower().Contains(word)
                            || u.RequesterName.ToString().Contains(word)
                            || u.FinalDecision.ToString().Contains(word)
                            || u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                // Sorting
                query = request.SortField switch
                {
                    "transactionNumber" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.TransactionNumber)
                        : query.OrderBy(u => u.TransactionNumber),
                    "policyName" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PolicyName)
                        : query.OrderBy(u => u.PolicyName),
                    "createdDate" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.CreatedDate)
                        : query.OrderBy(u => u.CreatedDate),
                    _ => query.OrderByDescending(u => u.CreatedDate),
                };

                var totalCount = await query.CountAsync();

                approvableTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId.ToString()
                        && a.Respond == 0
                        && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .ToListAsync();

                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;

                var items = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        TransactionId = h.TransactionId,
                        TransactionNumber = h.TransactionNumber,
                        PolicyName = h.PolicyName,
                        Status = h.Status,
                        RequesterName = h.RequesterName,
                        CreatedDate = h.CreatedDate,
                        RequesterId = h.RequesterId,
                        FinalDecision = h.FinalDecision,

                        CanApprove = approvableTransactionIds.Contains(h.TransactionId),
                        HasApproverResponded = _context.EpeVTransactionApprovers.Any(a =>
                            a.TransactionId == h.TransactionId && a.Respond > 0
                        ),
                    })
                    .ToListAsync();

                return Ok(
                    new
                    {
                        success = true,
                        items = items,
                        totalCount,
                        page,
                        pageSize,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("closed")]
        public async Task<IActionResult> GetExcemptionClosedList(
            [FromBody] ExemptionListRequest request,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var currentUserEmplId = request.UserId.ToString();

                // Transactions where the user is (or was) an approver on the transaction
                var approverTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .Distinct()
                    .ToListAsync();

                // Own closed requests OR closed requests where the user was an approver
                var query = _context.EpeVTransactionHeaders.Where(u =>
                    (u.FinalDecision == "Approved" || u.FinalDecision == "Disapproved")
                    && u.IsDelete == 0
                    && (
                        u.RequesterId == currentUserEmplId
                        || approverTransactionIds.Contains(u.TransactionId)
                    )
                );

                var searchTerm = request.Search ?? search;

                // Date range filtering
                if (
                    !string.IsNullOrWhiteSpace(request.StartDate)
                    && DateTime.TryParse(request.StartDate, out var startDate)
                )
                {
                    query = query.Where(u => u.CreatedDate >= startDate);
                }
                if (
                    !string.IsNullOrWhiteSpace(request.EndDate)
                    && DateTime.TryParse(request.EndDate, out var endDate)
                )
                {
                    query = query.Where(u => u.CreatedDate <= endDate);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchWords = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word)
                            || u.TransactionNumber.ToString().ToLower().Contains(word)
                            || u.RequesterName.ToString().Contains(word)
                            || u.FinalDecision.ToString().Contains(word)
                            || u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                query = request.SortField switch
                {
                    "transactionNumber" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.TransactionNumber)
                        : query.OrderBy(u => u.TransactionNumber),
                    "policyName" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PolicyName)
                        : query.OrderBy(u => u.PolicyName),
                    "createdDate" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.CreatedDate)
                        : query.OrderBy(u => u.CreatedDate),
                    _ => query.OrderByDescending(u => u.CreatedDate),
                };

                var totalCount = await query.CountAsync();

                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        TransactionId = h.TransactionId,
                        TransactionNumber = h.TransactionNumber,
                        PolicyName = h.PolicyName,
                        Status = h.Status,
                        RequesterName = h.RequesterName,
                        CreatedDate = h.CreatedDate,
                        RequesterId = h.RequesterId,
                        FinalDecision = h.FinalDecision,
                        CanApprove = false,
                        IsRequester = h.RequesterId == currentUserEmplId,
                    })
                    .ToListAsync();

                return Ok(
                    new
                    {
                        success = true,
                        items = items,
                        totalCount,
                        page,
                        pageSize,
                    }
                );
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
                var item = await _context
                    .EpeVTransactionHeaders.Where(u => u.TransactionId == id && u.IsDelete == 0)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return NotFound(
                        new { success = false, message = "Exemption request not found" }
                    );
                }
                var imHead = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == item.RequesterId)
                    .FirstOrDefaultAsync();

                object approversWithRole;

                // Drafts have no EpeTransactionapprover rows yet (those are only
                // created on submit). Preview the policy's configured approvers
                // instead, resolving IM dynamically off the requester.
                if (item.Status == "D")
                {
                    var policyApprovers = await _context
                        .EpePolicyapprovers.Where(p =>
                            p.Policyid == item.PolicyId && p.Isdelete == 0
                        )
                        .OrderBy(p => p.Linesequence)
                        .ThenBy(p => p.Sort)
                        .ToListAsync();

                    approversWithRole = policyApprovers
                        .Select(p => new
                        {
                            TransApproverId = 0L, // no transaction approver row exists yet
                            TransactionId = item.TransactionId,
                            PolicyApproverId = p.Id.ToString(),
                            EmployeeId = p.Approvertype == "IM" && imHead != null
                                ? imHead.ImmediateId
                                : p.Approverid,
                            ApproverName = p.Approvertype == "IM" && imHead != null
                                ? imHead.ImmediateName
                                : p.Approvername,
                            ApproverEmail = p.Approvertype == "IM" && imHead != null
                                ? imHead.ImmediateEmail
                                : p.Approveremail,
                            ApproverType = p.Approvertype,
                            LineSequence = p.Linesequence,
                            Sort = p.Sort,
                            Remarks = (string?)null,
                            Respond = 0,
                            RespondDate = (DateTime?)null,
                            IsSent = (byte)0,
                            SentDate = (DateTime?)null,
                            SentCount = 0,
                            LastSendDate = (DateTime?)null,
                            IsDelete = p.Isdelete,
                            CreateDate = p.Createddate,
                            IsMainApprover = p.Linesequence == 1,
                            ApproverRole = p.Linesequence == 1 ? "Main" : "Proxy",
                        })
                        .ToList();
                }
                else
                {
                    var approvers = await _context
                        .EpeVTransactionApprovers.Where(a => a.TransactionId == id)
                        .OrderBy(a => a.LineSequence)
                        .ThenBy(a => a.Sort)
                        .ToListAsync();

                    approversWithRole = approvers
                        .Select(a =>
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
                                ApproverRole = a.LineSequence == 1 ? "Main" : "Proxy",
                            };
                        })
                        .ToList();
                }

                return Ok(
                    new
                    {
                        success = true,
                        item,
                        approvers = approversWithRole,
                    }
                );
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
                var item = await _context
                    .EpeVTransactionHeaders.Where(u => u.TransactionId == id && u.IsDelete == 0)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return NotFound(
                        new { success = false, message = "Exemption request not found" }
                    );
                }

                var imHead = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == item.RequesterId)
                    .FirstOrDefaultAsync();

                object approversWithRole;

                // Drafts have no EpeTransactionapprover rows yet (those are only
                // created on submit). Preview the policy's configured approvers
                // instead, resolving IM dynamically off the requester.
                if (item.Status == "D")
                {
                    var policyApprovers = await _context
                        .EpePolicyapprovers.Where(p =>
                            p.Policyid == item.PolicyId && p.Isdelete == 0
                        )
                        .OrderBy(p => p.Linesequence)
                        .ThenBy(p => p.Sort)
                        .ToListAsync();

                    approversWithRole = policyApprovers
                        .Select(p => new
                        {
                            TransApproverId = 0L, // no transaction approver row exists yet
                            TransactionId = item.TransactionId,
                            PolicyApproverId = p.Id.ToString(),
                            EmployeeId = p.Approvertype == "IM" && imHead != null
                                ? imHead.ImmediateId
                                : p.Approverid,
                            ApproverName = p.Approvertype == "IM" && imHead != null
                                ? imHead.ImmediateName
                                : p.Approvername,
                            ApproverEmail = p.Approvertype == "IM" && imHead != null
                                ? imHead.ImmediateEmail
                                : p.Approveremail,
                            ApproverType = p.Approvertype,
                            LineSequence = p.Linesequence,
                            Sort = p.Sort,
                            Remarks = (string?)null,
                            Respond = 0,
                            RespondDate = (DateTime?)null,
                            IsSent = (byte)0,
                            SentDate = (DateTime?)null,
                            SentCount = 0,
                            LastSendDate = (DateTime?)null,
                            IsDelete = p.Isdelete,
                            CreateDate = p.Createddate,
                            IsMainApprover = p.Linesequence == 1,
                            ApproverRole = p.Linesequence == 1 ? "Main" : "Proxy",
                        })
                        .ToList();
                }
                else
                {
                    var approvers = await _context
                        .EpeVTransactionApprovers.Where(a => a.TransactionId == id)
                        .OrderBy(a => a.LineSequence)
                        .ThenBy(a => a.Sort)
                        .ToListAsync();

                    approversWithRole = approvers
                        .Select(a =>
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
                                ApproverRole = a.LineSequence == 1 ? "Main" : "Proxy",
                            };
                        })
                        .ToList();
                }

                return Ok(
                    new
                    {
                        success = true,
                        item,
                        approvers = approversWithRole,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("save-draft")]
        public async Task<IActionResult> SaveDraft([FromForm] DraftExemptionRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var createdBy = request.Modifiedby;
                var requesterId = request.Requestorid;

                var userDetails = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == requesterId)
                    .FirstOrDefaultAsync();

                var transactionHeader = new EpeTransactionheader
                {
                    Policyid = request.Policyid ?? 0, // Allow null for drafts
                    Details = request.Details,
                    Corporate = userDetails?.Corporate,
                    Requestorid = requesterId,
                    Finaldecision = "Draft",
                    Status = "D",
                    Createdby = createdBy.HasValue ? createdBy.Value : 0,
                    Createddate = DateTime.UtcNow.AddHours(8),
                };

                // Client IP from headers
                var ipAddress =
                    HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                    ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "Unknown";

                // MAC address � server's own NIC (not the client's)
                var macAddress =
                    NetworkInterface
                        .GetAllNetworkInterfaces()
                        .FirstOrDefault(n =>
                            n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                        )
                        ?.GetPhysicalAddress()
                        .ToString()
                    ?? "Unknown";

                request.Ipaddress = ipAddress;
                request.Macaddress = macAddress;

                _context.EpeTransactionheaders.Add(transactionHeader);

                await _context.SaveChangesAsync();
                var userId = _context
                    .CoreVUsers.Where(u => u.EmplId == requesterId)
                    .Select(a => a.UserId)
                    .FirstOrDefault();

                await LogTransactionAsync(
                    transactionHeader.Transactionid,
                    transactionHeader.Policyid,
                    "DRAFT",
                    "N",
                    "D",
                    userId
                );

                // Handle attachments (same as create)
                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var attachment in request.Attachments)
                    {
                        var savedFileName = await _fileUploadService.SaveFile(attachment.File);
                        var epeAttachment = new EpeAttachment
                        {
                            TaskId = transactionHeader.Transactionnumber,
                            // ReferenceType = attachment.ReferenceType,
                            UploadFilename = attachment.File.FileName,
                            Newfilename = savedFileName,
                            Filetype = Path.GetExtension(attachment.File.FileName).TrimStart('.'),
                            Createdate = DateTime.UtcNow.AddHours(8),
                            Computerhost = HttpContext.Request.Host.Host,
                        };
                        _context.EpeAttachments.Add(epeAttachment);
                    }
                    await _context.SaveChangesAsync();
                }

                // No approvers or emails for drafts

                await transaction.CommitAsync();

                return Ok(
                    new
                    {
                        success = true,
                        message = "Draft saved successfully",
                        item = transactionHeader,
                    }
                );
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error " + e.Message);
            }
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateExemptionRequest(
            [FromForm] CreateExemptionRequestDto request
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ============================================================
                // DEBUG
                // ============================================================

                _logger.LogWarning("Form keys: {Keys}", string.Join(",", Request.Form.Keys));

                _logger.LogWarning("Files count: {Count}", Request.Form.Files.Count);

                // ============================================================
                // REQUESTER
                // ============================================================

                var createdBy = request.Createdby;
                var requesterId = request.Requestorid;

                var userDetails = await _context.CoreVEmployeeDetails.FirstOrDefaultAsync(e =>
                    e.EmplId == requesterId
                );

                // ============================================================
                // CREATE TRANSACTION HEADER
                // ============================================================

                var transactionHeader = new EpeTransactionheader
                {
                    Policyid = request.Policyid,
                    Details = request.Details,
                    Corporate = userDetails?.Corporate,
                    Requestorid = requesterId,

                    Finaldecision = "Pending",

                    Createdby = createdBy ?? 0,
                    Createddate = DateTime.UtcNow.AddHours(8),
                };

                _context.EpeTransactionheaders.Add(transactionHeader);

                await _context.SaveChangesAsync();

                // ============================================================
                // GET REQUESTER USER ID
                // ============================================================

                var userId = 0;

                if (!string.IsNullOrEmpty(userDetails?.EmplId))
                {
                    userId = await _context
                        .CoreVUsers.Where(u => u.EmplId == userDetails.EmplId)
                        .Select(u => u.UserId)
                        .FirstOrDefaultAsync();
                }

                // ============================================================
                // LOG CREATE
                // ============================================================

                await LogTransactionAsync(
                    transactionHeader.Transactionid,
                    transactionHeader.Policyid,
                    "CREATE",
                    "N",
                    "O",
                    userId
                );

                // ============================================================
                // GET POLICY APPROVERS
                // ============================================================

                var policyApprovers = await _context
                    .EpePolicyapprovers.Where(p =>
                        p.Policyid == request.Policyid && p.Isdelete == 0
                    )
                    .OrderBy(p => p.Linesequence)
                    .ThenBy(p => p.Sort)
                    .ToListAsync();

                // ============================================================
                // FIND REQUESTER'S SORT GROUP
                //
                // We DO NOT remove these approvers.
                //
                // This is only used to determine which Sort should be skipped
                // when sending the first approval email.
                // ============================================================

                var requesterSortGroups = policyApprovers
                    .Where(p => p.Approverid == requesterId.ToString())
                    .Select(p => p.Sort)
                    .ToHashSet();

                // ============================================================
                // CREATE TRANSACTION APPROVERS
                //
                // KEEP:
                //   - All approvers
                //   - Original Sort
                //   - Original Linesequence
                //
                // REQUESTER:
                //   Respond = 1
                //
                // OTHER APPROVERS:
                //   Respond = 0
                // ============================================================

                foreach (var policyApprover in policyApprovers)
                {
                    bool isRequester = policyApprover.Approverid == requesterId.ToString();

                    _context.EpeTransactionapprovers.Add(
                        new EpeTransactionapprover
                        {
                            Transactionid = transactionHeader.Transactionid,

                            Approverid = policyApprover.Id.ToString(),

                            Approvertype = policyApprover.Approvertype,

                            // KEEP ORIGINAL VALUES
                            Linesequence = policyApprover.Linesequence,

                            Sort = policyApprover.Sort,

                            // REQUESTER IS AUTOMATICALLY APPROVED
                            Respond = isRequester ? 1 : 0,

                            Issent = 0,
                            Sentcount = 0,
                            Isdelete = 0,

                            Createddate = DateTime.UtcNow.AddHours(8),
                            Responddate = isRequester ? DateTime.UtcNow.AddHours(8) : (DateTime?)null,
                        }
                    );
                }

                await _context.SaveChangesAsync();

                // ============================================================
                // SAVE ATTACHMENTS
                // ============================================================

                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var attachment in request.Attachments)
                    {
                        if (attachment?.File == null)
                            continue;

                        var savedFileName = await _fileUploadService.SaveFile(attachment.File);

                        _context.EpeAttachments.Add(
                            new EpeAttachment
                            {
                                TaskId = transactionHeader.Transactionnumber,

                                UploadFilename = attachment.File.FileName,

                                Newfilename = savedFileName,

                                Filetype = Path.GetExtension(attachment.File.FileName)
                                    .TrimStart('.'),

                                Createdate = DateTime.UtcNow.AddHours(8),

                                Computerhost = HttpContext.Request.Host.Host,
                            }
                        );
                    }

                    await _context.SaveChangesAsync();
                }

                // ============================================================
                // DETERMINE SORT GROUPS
                // ============================================================

                var sortGroups = policyApprovers
                    .Select(p => p.Sort)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                // ============================================================
                // ONLY ONE SORT GROUP
                //
                // There is no next approval group.
                //
                // Therefore the transaction is immediately closed
                // as Approved.
                // ============================================================

                if (sortGroups.Count == 1)
                {
                    transactionHeader.Status = "C";
                    transactionHeader.Finaldecision = "Approved";
                    transactionHeader.Modifieddate = DateTime.UtcNow.AddHours(8);

                    await LogTransactionAsync(
                        transactionHeader.Transactionid,
                        transactionHeader.Policyid,
                        "CLOSE",
                        "O",
                        "C",
                        userId
                    );

                    await _context.SaveChangesAsync();
                }
                else
                {
                    // ========================================================
                    // MULTIPLE SORT GROUPS
                    //
                    // Skip requester's Sort group and find the next Sort.
                    // ========================================================

                    var activeApprovers = policyApprovers
                        .Where(p => !requesterSortGroups.Contains(p.Sort))
                        .ToList();

                    if (activeApprovers.Any())
                    {
                        // ----------------------------------------------------
                        // FIRST ACTIVE SORT
                        // ----------------------------------------------------

                        var firstSort = activeApprovers.Min(p => p.Sort);

                        // ----------------------------------------------------
                        // FIRST APPROVAL GROUP
                        // ----------------------------------------------------

                        var firstGroupApprovers = activeApprovers
                            .Where(p => p.Sort == firstSort)
                            .OrderBy(p => p.Linesequence ?? int.MaxValue)
                            .ToList();

                        // ----------------------------------------------------
                        // SEND EMAILS
                        // ----------------------------------------------------

                        foreach (var approver in firstGroupApprovers)
                        {
                            string approverEmail = approver.Approveremail ?? "";

                            string approverName = approver.Approvername ?? "";

                            int approverUserId = 0;

                            bool isProxy = approver.Linesequence != 1;

                            // ------------------------------------------------
                            // IMMEDIATE MANAGER
                            // ------------------------------------------------

                            if (approver.Approvertype == "IM" && userDetails != null)
                            {
                                approverEmail = userDetails.ImmediateEmail ?? "";

                                approverName = userDetails.ImmediateName ?? "";
                            }

                            if (!string.IsNullOrEmpty(approverEmail))
                            {
                                try
                                {
                                    // ========================================
                                    // GET APPROVER USER ID
                                    // ========================================

                                    if (!string.IsNullOrEmpty(approver.Approverid))
                                    {
                                        var approverUser =
                                            await _context.CoreVUsers.FirstOrDefaultAsync(u =>
                                                u.EmplId == approver.Approverid
                                            );

                                        if (approverUser != null)
                                        {
                                            approverUserId = approverUser.UserId;
                                        }
                                    }

                                    // ========================================
                                    // FALLBACK FOR IM
                                    // ========================================

                                    if (
                                        approverUserId == 0
                                        && approver.Approvertype == "IM"
                                        && userDetails != null
                                        && !string.IsNullOrEmpty(userDetails.ImmediateId)
                                    )
                                    {
                                        var imUser = await _context.CoreVUsers.FirstOrDefaultAsync(
                                            u => u.EmplId == userDetails.ImmediateId
                                        );

                                        if (imUser != null)
                                        {
                                            approverUserId = imUser.UserId;
                                        }
                                    }

                                    // ========================================
                                    // GET TRANSACTION APPROVER
                                    // ========================================

                                    var transApproverRecord = await _context
                                        .EpeTransactionapprovers.Where(a =>
                                            a.Transactionid == transactionHeader.Transactionid
                                            && a.Approverid == approver.Id.ToString()
                                            && a.Isdelete == 0
                                        )
                                        .FirstOrDefaultAsync();

                                    // ========================================
                                    // PROXY EMAIL
                                    // ========================================

                                    if (isProxy)
                                    {
                                        await _emailService.SendProxyApproverReminderEmailAsync(
                                            approverEmail,
                                            approverName,
                                            firstGroupApprovers.First().Approvername
                                                ?? "Main Approver",
                                            transactionHeader.Transactionid,
                                            transactionHeader.Transactionnumber,
                                            userDetails?.Employeename,
                                            transactionHeader.Details ?? "No details provided",
                                            approverUserId,
                                            transApproverRecord?.Transapproverid ?? 0
                                        );
                                    }
                                    else
                                    {
                                        // ====================================
                                        // NORMAL APPROVER EMAIL
                                        // ====================================

                                        await _emailService.SendApprovalRequestEmailAsync(
                                            approverEmail,
                                            transactionHeader.Transactionid,
                                            transactionHeader.Transactionnumber,
                                            userDetails?.Employeename,
                                            transactionHeader.Details ?? "No details provided",
                                            approverUserId,
                                            transApproverRecord?.Transapproverid ?? 0
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(
                                        ex,
                                        "Failed to send approval request email to {Email}",
                                        approverEmail
                                    );
                                }
                            }
                        }
                    }
                }

                // ============================================================
                // COMMIT
                // ============================================================

                await transaction.CommitAsync();

                // ============================================================
                // SEND REQUEST CREATED EMAIL
                // ============================================================

                if (!string.IsNullOrEmpty(userDetails?.Emailadd))
                {
                    try
                    {
                        await _emailService.SendRequestCreatedEmailAsync(
                            userDetails.Emailadd,
                            transactionHeader.Transactionid,
                            transactionHeader.Transactionnumber,
                            transactionHeader.Details ?? "No details provided",
                            userId
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to send request created email to requester {Email}",
                            userDetails.Emailadd
                        );
                    }
                }

                // ============================================================
                // RESPONSE
                // ============================================================

                return Ok(
                    new
                    {
                        success = true,

                        message = "Exemption request created successfully",

                        item = transactionHeader,
                    }
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "CreateExemptionRequest failed");

                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("update-draft/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateDraft(
            int id,
            [FromForm] DraftExemptionRequestDto request
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingItem = await _context
                    .EpeTransactionheaders.Where(u =>
                        u.Transactionid == id && u.Status == "D" && u.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (existingItem == null)
                {
                    return NotFound(new { success = false, message = "Draft not found" });
                }

                // Only allow updating these fields for drafts
                existingItem.Policyid =
                    request.Policyid != 0 ? request.Policyid.Value : existingItem.Policyid;
                existingItem.Details = request.Details ?? existingItem.Details;
                existingItem.Modifieddate = DateTime.UtcNow.AddHours(8);
                existingItem.Modifiedby = request.Modifiedby;

                await _context.SaveChangesAsync();

                // Handle attachments (same as save-draft)
                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var attachment in request.Attachments)
                    {
                        if (attachment?.File == null)
                            continue;

                        var savedFileName = await _fileUploadService.SaveFile(attachment.File);
                        var epeAttachment = new EpeAttachment
                        {
                            TaskId = existingItem.Transactionnumber,
                            UploadFilename = attachment.File.FileName,
                            Newfilename = savedFileName,
                            Filetype = Path.GetExtension(attachment.File.FileName).TrimStart('.'),
                            Createdate = DateTime.UtcNow.AddHours(8),
                            Computerhost = HttpContext.Request.Host.Host,
                        };
                        _context.EpeAttachments.Add(epeAttachment);
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Ok(
                    new
                    {
                        success = true,
                        message = "Draft updated successfully",
                        item = existingItem,
                    }
                );
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error " + e.Message);
            }
        }

        [HttpPost("submit-draft/{id}")]
        public async Task<IActionResult> SubmitDraft(int id, [FromBody] ApproverRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ============================================================
                // GET DRAFT
                // ============================================================

                var draft = await _context
                    .EpeTransactionheaders.Where(u =>
                        u.Transactionid == id && u.Status == "D" && u.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (draft == null)
                {
                    return NotFound(new { success = false, message = "Draft not found" });
                }

                // ============================================================
                // VALIDATE REQUIRED FIELDS
                // ============================================================

                if (draft.Policyid == 0 || string.IsNullOrEmpty(draft.Details))
                {
                    return BadRequest(
                        new
                        {
                            success = false,
                            message = "Draft must have policy and details to submit",
                        }
                    );
                }

                // ============================================================
                // CHANGE STATUS TO OPEN
                // ============================================================

                draft.Status = "O";
                draft.Finaldecision = "Pending";
                draft.Modifieddate = DateTime.UtcNow.AddHours(8);

                // ============================================================
                // GET REQUESTER USER ID
                // ============================================================

                var requester = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == draft.Requestorid)
                    .FirstOrDefaultAsync();

                var requesterUserId = 0;

                if (requester != null && !string.IsNullOrEmpty(requester.EmplId))
                {
                    requesterUserId = await _context
                        .CoreVUsers.Where(u => u.EmplId == requester.EmplId)
                        .Select(u => u.UserId)
                        .FirstOrDefaultAsync();
                }

                // ============================================================
                // LOG SUBMIT
                // ============================================================

                await LogTransactionAsync(
                    draft.Transactionid,
                    draft.Policyid,
                    "SUBMIT",
                    "D",
                    "O",
                    request.UserId
                );

                await _context.SaveChangesAsync();

                // ============================================================
                // GET POLICY APPROVERS
                // ============================================================

                var policyApprovers = await _context
                    .EpePolicyapprovers.Where(p => p.Policyid == draft.Policyid && p.Isdelete == 0)
                    .OrderBy(p => p.Linesequence)
                    .ThenBy(p => p.Sort)
                    .ToListAsync();

                // ============================================================
                // FIND REQUESTER'S SORT GROUP
                //
                // This is ONLY used to determine which group to skip
                // for the initial email.
                // ============================================================

                var requesterSortGroups = policyApprovers
                    .Where(p => p.Approverid == draft.Requestorid.ToString())
                    .Select(p => p.Sort)
                    .ToHashSet();

                // ============================================================
                // CREATE TRANSACTION APPROVERS
                //
                // KEEP ALL APPROVERS
                // KEEP ORIGINAL SORT
                // KEEP ORIGINAL LINESEQUENCE
                //
                // REQUESTER = RESPOND 1
                // OTHERS    = RESPOND 0
                // ============================================================

                foreach (var policyApprover in policyApprovers)
                {
                    bool isRequester = policyApprover.Approverid == draft.Requestorid.ToString();

                    _context.EpeTransactionapprovers.Add(
                        new EpeTransactionapprover
                        {
                            Transactionid = draft.Transactionid,

                            Approverid = policyApprover.Id.ToString(),

                            Approvertype = policyApprover.Approvertype,

                            // KEEP ORIGINAL VALUES
                            Linesequence = policyApprover.Linesequence,

                            Sort = policyApprover.Sort,

                            // REQUESTER IS AUTOMATICALLY APPROVED
                            Respond = isRequester ? 1 : 0,

                            Issent = 0,
                            Sentcount = 0,
                            Isdelete = 0,

                            Createddate = DateTime.UtcNow.AddHours(8),
                            Responddate = isRequester ? DateTime.UtcNow.AddHours(8) : (DateTime?)null,
                        }
                    );
                }

                await _context.SaveChangesAsync();

                // ============================================================
                // DETERMINE SORT GROUPS
                // ============================================================

                var sortGroups = policyApprovers
                    .Select(p => p.Sort)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                // ============================================================
                // ONLY ONE SORT GROUP
                //
                // There is no next approval group.
                //
                // Close transaction immediately as Approved.
                // ============================================================

                if (sortGroups.Count == 1)
                {
                    draft.Status = "C";
                    draft.Finaldecision = "Approved";
                    draft.Modifieddate = DateTime.UtcNow.AddHours(8);

                    // --------------------------------------------------------
                    // LOG CLOSE
                    // --------------------------------------------------------

                    await LogTransactionAsync(
                        draft.Transactionid,
                        draft.Policyid,
                        "CLOSE",
                        "O",
                        "C",
                        request.UserId
                    );

                    await _context.SaveChangesAsync();
                }
                else
                {
                    // ========================================================
                    // MULTIPLE SORT GROUPS
                    //
                    // Skip requester's Sort and find next approval group.
                    // ========================================================

                    var activeApprovers = policyApprovers
                        .Where(p => !requesterSortGroups.Contains(p.Sort))
                        .ToList();

                    if (activeApprovers.Any())
                    {
                        // ----------------------------------------------------
                        // FIRST ACTIVE SORT
                        // ----------------------------------------------------

                        var firstSort = activeApprovers.Min(p => p.Sort);

                        // ----------------------------------------------------
                        // FIRST APPROVAL GROUP
                        // ----------------------------------------------------

                        var firstGroupApprovers = activeApprovers
                            .Where(p => p.Sort == firstSort)
                            .OrderBy(p => p.Linesequence ?? int.MaxValue)
                            .ToList();

                        // ----------------------------------------------------
                        // SEND EMAILS
                        // ----------------------------------------------------

                        foreach (var approver in firstGroupApprovers)
                        {
                            string approverEmail = approver.Approveremail ?? "";

                            string approverName = approver.Approvername ?? "";

                            int approverUserId = 0;

                            bool isProxy = approver.Linesequence != 1;

                            // ------------------------------------------------
                            // IMMEDIATE MANAGER
                            // ------------------------------------------------

                            if (approver.Approvertype == "IM" && requester != null)
                            {
                                approverEmail = requester.ImmediateEmail ?? "";

                                approverName = requester.ImmediateName ?? "";
                            }

                            if (!string.IsNullOrEmpty(approverEmail))
                            {
                                try
                                {
                                    // ========================================
                                    // GET APPROVER USER ID
                                    // ========================================

                                    if (!string.IsNullOrEmpty(approver.Approverid))
                                    {
                                        var approverUser =
                                            await _context.CoreVUsers.FirstOrDefaultAsync(u =>
                                                u.EmplId == approver.Approverid
                                            );

                                        if (approverUser != null)
                                        {
                                            approverUserId = approverUser.UserId;
                                        }
                                    }

                                    // ========================================
                                    // FALLBACK FOR IM
                                    // ========================================

                                    if (
                                        approverUserId == 0
                                        && approver.Approvertype == "IM"
                                        && requester != null
                                        && !string.IsNullOrEmpty(requester.ImmediateId)
                                    )
                                    {
                                        var imUser = await _context.CoreVUsers.FirstOrDefaultAsync(
                                            u => u.EmplId == requester.ImmediateId
                                        );

                                        if (imUser != null)
                                        {
                                            approverUserId = imUser.UserId;
                                        }
                                    }

                                    // ========================================
                                    // GET TRANSACTION APPROVER
                                    // ========================================

                                    var transApproverRecord = await _context
                                        .EpeTransactionapprovers.Where(a =>
                                            a.Transactionid == draft.Transactionid
                                            && a.Approverid == approver.Id.ToString()
                                            && a.Isdelete == 0
                                        )
                                        .FirstOrDefaultAsync();

                                    // ========================================
                                    // PROXY EMAIL
                                    // ========================================

                                    if (isProxy)
                                    {
                                        await _emailService.SendProxyApproverReminderEmailAsync(
                                            approverEmail,
                                            approverName,
                                            firstGroupApprovers.First().Approvername
                                                ?? "Main Approver",
                                            draft.Transactionid,
                                            draft.Transactionnumber,
                                            requester?.Employeename,
                                            draft.Details ?? "No details provided",
                                            approverUserId,
                                            transApproverRecord?.Transapproverid ?? 0
                                        );
                                    }
                                    else
                                    {
                                        // ====================================
                                        // NORMAL APPROVER EMAIL
                                        // ====================================

                                        await _emailService.SendApprovalRequestEmailAsync(
                                            approverEmail,
                                            draft.Transactionid,
                                            draft.Transactionnumber,
                                            requester?.Employeename,
                                            draft.Details ?? "No details provided",
                                            approverUserId,
                                            transApproverRecord?.Transapproverid ?? 0
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(
                                        ex,
                                        "Failed to send approval request email to {Email}",
                                        approverEmail
                                    );
                                }
                            }
                        }
                    }
                }

                // ============================================================
                // COMMIT
                // ============================================================

                await transaction.CommitAsync();

                // ============================================================
                // SEND REQUEST CREATED EMAIL
                // ============================================================

                if (requester != null && !string.IsNullOrEmpty(requester.Emailadd))
                {
                    try
                    {
                        await _emailService.SendRequestCreatedEmailAsync(
                            requester.Emailadd,
                            draft.Transactionid,
                            draft.Transactionnumber,
                            draft.Details ?? "No details provided",
                            requesterUserId
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to send request created email to requester {Email}",
                            requester.Emailadd
                        );
                    }
                }

                // ============================================================
                // RESPONSE
                // ============================================================

                return Ok(
                    new
                    {
                        success = true,

                        message = "Draft submitted successfully",

                        item = draft,
                    }
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "SubmitDraft failed for transaction {TransactionId}", id);

                return StatusCode(500, "Error " + ex.Message);
            }
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateExemptionRequest(
            int id,
            [FromBody] EpeTransactionheader request
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingItem = await _context
                    .EpeTransactionheaders.Where(u => u.Transactionid == id && u.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (existingItem == null)
                {
                    return NotFound(
                        new { success = false, message = "Exemption request not found" }
                    );
                }

                // Only allow editing these fields: Corporate, PolicyId, Details
                // Preserve system fields: Status, FinalDecision, CreatedBy, CreatedDate
                existingItem.Corporate = request.Corporate;
                existingItem.Policyid = request.Policyid;
                existingItem.Details = request.Details;
                existingItem.Modifieddate = DateTime.UtcNow.AddHours(8);
                existingItem.Modifiedby = request.Modifiedby;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(
                    new
                    {
                        success = true,
                        message = "Exemption request updated successfully",
                        item = existingItem,
                    }
                );
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteExemptionRequest(
            int id,
            [FromBody] ApproverRequest request
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingItem = await _context
                    .EpeTransactionheaders.Where(u => u.Transactionid == id && u.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (existingItem == null)
                {
                    return NotFound(
                        new { success = false, message = "Exemption request not found" }
                    );
                }

                // Soft delete - mark as deleted
                //existingItem.ExecuteUpdateAsync(s => s
                //    .SetProperty(h => h.Isdelete, 1)
                //    .SetProperty(h => h.Modifiedby, request.UserId)
                //    .SetProperty(h => h.Modifieddate, DateTime.UtcNow.AddHours(8))
                //);

                existingItem.Isdelete = 1;
                existingItem.Modifiedby = request.UserId;
                existingItem.Modifieddate = DateTime.UtcNow.AddHours(8);

                var exsistingApprovals = await _context
                    .EpeTransactionapprovers.Where(a => a.Transactionid == id && a.Isdelete == 0)
                    .ExecuteUpdateAsync(b => b.SetProperty(a => a.Isdelete, 1));

                await LogTransactionAsync(
                    id,
                    existingItem.Policyid,
                    "DELETE",
                    existingItem.Status,
                    "X",
                    request.UserId
                );
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(
                    new { success = true, message = "Exemption request deleted successfully" }
                );
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("closed/export")]
        public async Task<IActionResult> ExportClosedExemptions(
            [FromBody] ExemptionListRequest request
        )
        {
            try
            {
                var query = _context.EpeVTransactionHeaders.Where(u =>
                    (u.FinalDecision == "Approved" || u.FinalDecision == "Disapproved")
                    && u.IsDelete == 0
                );

                // Date range filtering
                if (
                    !string.IsNullOrWhiteSpace(request.StartDate)
                    && DateTime.TryParse(request.StartDate, out var startDate)
                )
                {
                    query = query.Where(u => u.CreatedDate >= startDate.Date);
                }
                if (DateTime.TryParse(request.EndDate, out var endDate))
                {
                    query = query.Where(u => u.CreatedDate <= endDate.Date.AddDays(1));
                }

                var items = await query
                    .Select(h => new
                    {
                        h.TransactionId,
                        h.TransactionNumber,
                        h.PolicyName,
                        h.Status,
                        h.RequesterName,
                        CreatedDate = h.CreatedDate.ToString("MMM d, yyyy h:mm tt"),
                        h.FinalDecision,
                    })
                    .ToListAsync();

                return Ok(new { success = true, items });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("respond")]
        public async Task<IActionResult> RespondToApproval(
            [FromBody] ApproverResponseRequest request
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transactionHeader = await _context
                    .EpeTransactionheaders.Where(u =>
                        u.Transactionid == request.TransactionId && u.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (transactionHeader == null)
                    return NotFound(
                        new { success = false, message = "Exemption request not found" }
                    );

                var approver = await _context
                    .EpeTransactionapprovers.Where(a =>
                        a.Transapproverid == request.TransApproverId && a.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (approver == null)
                    return NotFound(new { success = false, message = "Approver not found" });

                var requester = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == transactionHeader.Requestorid)
                    .FirstOrDefaultAsync();

                approver.Respond = request.Respond;
                var respond = "";
                if (request.Respond == 1)
                {
                    respond = "A";
                }
                else if (request.Respond == 2)
                {
                    respond = "Z";
                }
                approver.Responddate = DateTime.UtcNow.AddHours(8);
                approver.Remarks = request.Remarks; // Individual remarks for this approver

                var userId = _context
                    .CoreVUsers.Where(u => u.EmplId == request.EmployeeId)
                    .Select(e => e.UserId)
                    .FirstOrDefault();

                await LogTransactionAsync(
                    transactionHeader.Transactionid,
                    transactionHeader.Policyid,
                    "RESPOND",
                    "P",
                    respond,
                    userId
                );
                await _context.SaveChangesAsync();

                if (request.Respond == 1) // Approved
                {
                    // Reload all approvers fresh after the update
                    var allApprovers = await _context
                        .EpeTransactionapprovers.Where(a =>
                            a.Transactionid == request.TransactionId && a.Isdelete == 0
                        )
                        .OrderBy(a => a.Sort)
                        .ThenBy(a => a.Linesequence)
                        .ToListAsync();

                    var currentApprover = allApprovers.First(a =>
                        a.Transapproverid == request.TransApproverId
                    );

                    // Reload all approvers fresh after the update
                    var updatedApprovers = await _context
                        .EpeTransactionapprovers.Where(a =>
                            a.Transactionid == request.TransactionId && a.Isdelete == 0
                        )
                        .OrderBy(a => a.Sort)
                        .ThenBy(a => a.Linesequence)
                        .ToListAsync();

                    // Get the current approver's sort value
                    var currentSort = currentApprover.Sort;

                    // Next approvers are those whose Sort is greater than current, and are still pending
                    var nextApprovers = updatedApprovers
                        .Where(a => a.Respond == 0)
                        .GroupBy(a => a.Sort ?? 0)
                        .Where(g => g.Key > (currentSort ?? 0))
                        .OrderBy(g => g.Key)
                        .FirstOrDefault()
                        ?.ToList();

                    if (nextApprovers != null && nextApprovers.Any())
                    {
                        // More approvers ahead � keep status as Pending
                        await _context
                            .EpeTransactionheaders.Where(h =>
                                h.Transactionid == request.TransactionId
                            )
                            .ExecuteUpdateAsync(b => b.SetProperty(h => h.Status, "P"));

                        // Notify the next group (main + proxies)
                        foreach (var nextApprover in nextApprovers)
                        {
                            string approverEmail = "";
                            string approverName = "";
                            int approverUserId = 0;
                            bool isProxy =
                                nextApprover.Linesequence != nextApprovers.Min(a => a.Linesequence);

                            var policyApprover = await _context
                                .EpePolicyapprovers.Where(e =>
                                    e.Id == int.Parse(nextApprover.Approverid)
                                )
                                .FirstOrDefaultAsync();

                            if (nextApprover.Approvertype == "IM" && requester != null)
                            {
                                approverEmail = requester.ImmediateEmail ?? "";
                                approverName = requester.ImmediateName ?? "";
                            }
                            else
                            {
                                approverEmail = policyApprover?.Approveremail ?? "";
                                approverName = policyApprover?.Approvername ?? "";
                            }

                            if (!string.IsNullOrEmpty(approverEmail))
                            {
                                try
                                {
                                    if (!string.IsNullOrEmpty(nextApprover.Approverid))
                                    {
                                        var approverUser =
                                            await _context.CoreVUsers.FirstOrDefaultAsync(u =>
                                                u.EmplId == (policyApprover.Approverid ?? "")
                                            );
                                        if (approverUser != null)
                                            approverUserId = approverUser.UserId;
                                    }

                                    var transApproverRecord = await _context
                                        .EpeTransactionapprovers.Where(a =>
                                            a.Transactionid == transactionHeader.Transactionid
                                            && a.Transapproverid == nextApprover.Transapproverid
                                            && a.Isdelete == 0
                                        )
                                        .FirstOrDefaultAsync();

                                    if (isProxy)
                                    {
                                        // Get the main approver name in this next group
                                        var mainInNextGroup = nextApprovers.FirstOrDefault(a =>
                                            a.Linesequence == nextApprovers.Min(x => x.Linesequence)
                                        );

                                        var mainPolicyApprover =
                                            mainInNextGroup != null
                                                ? await _context
                                                    .EpePolicyapprovers.Where(e =>
                                                        e.Id
                                                        == int.Parse(mainInNextGroup.Approverid)
                                                    )
                                                    .FirstOrDefaultAsync()
                                                : null;

                                        string mainApproverName =
                                            mainInNextGroup?.Approvertype == "IM"
                                            && requester != null
                                                ? requester.ImmediateName ?? "Immediate Head"
                                                : mainPolicyApprover?.Approvername
                                                    ?? "Main Approver";

                                        await _emailService.SendProxyApproverReminderEmailAsync(
                                            approverEmail,
                                            approverName,
                                            mainApproverName,
                                            transactionHeader.Transactionid,
                                            transactionHeader.Transactionnumber,
                                            requester?.Employeename
                                                ?? transactionHeader.Requestorid,
                                            transactionHeader.Details ?? "No details provided",
                                            approverUserId,
                                            transApproverRecord?.Transapproverid ?? 0
                                        );
                                    }
                                    else
                                    {
                                        await _emailService.SendMainApproverReminderEmailAsync(
                                            approverEmail,
                                            approverName,
                                            transactionHeader.Transactionid,
                                            transactionHeader.Transactionnumber,
                                            requester?.Employeename
                                                ?? transactionHeader.Requestorid,
                                            transactionHeader.Details ?? "No details provided",
                                            approverUserId,
                                            transApproverRecord?.Transapproverid ?? 0
                                        );
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(
                                        ex,
                                        "Failed to send next approver email to {Email}",
                                        approverEmail
                                    );
                                }
                            }
                        }
                    }
                    else
                    {
                        // No more pending approvers - fully approved
                        await _context
                            .EpeTransactionheaders.Where(h =>
                                h.Transactionid == request.TransactionId
                            )
                            .ExecuteUpdateAsync(b =>
                                b.SetProperty(h => h.Status, "C")
                                    .SetProperty(h => h.Finaldecision, "Approved")
                            );

                        await LogTransactionAsync(
                            transactionHeader.Transactionid,
                            transactionHeader.Policyid,
                            "CLOSE",
                            transactionHeader.Status,
                            "C",
                            userId
                        );

                        if (requester != null && !string.IsNullOrEmpty(requester.Emailadd))
                        {
                            try
                            {
                                var requesterUserId = 0;
                                var user = await _context.CoreVUsers.FirstOrDefaultAsync(u =>
                                    u.EmplId == requester.EmplId
                                );
                                if (user != null)
                                    requesterUserId = user.UserId;

                                await _emailService.SendStatusChangedEmailAsync(
                                    requester.Emailadd,
                                    transactionHeader.Transactionid,
                                    transactionHeader.Transactionnumber,
                                    "Pending",
                                    "Approved",
                                    "System",
                                    requesterUserId
                                );
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    ex,
                                    "Failed to send approval email to {Email}",
                                    requester.Emailadd
                                );
                            }
                        }
                    }
                }
                else if (request.Respond == 2) // Disapproved
                {
                    // Auto-disapprove everyone in the same sort group
                    var allApprovers = await _context
                        .EpeTransactionapprovers.Where(a =>
                            a.Transactionid == request.TransactionId && a.Isdelete == 0
                        )
                        .ToListAsync();

                    var groupIds = allApprovers
                        .Where(a => a.Sort == approver.Sort && a.Respond == 0)
                        .Select(a => a.Transapproverid)
                        .ToList();

                    // Disapprove the current approver individually
                    approver.Respond = 2;
                    approver.Responddate = DateTime.UtcNow.AddHours(8);
                    approver.Remarks = request.Remarks; // Already saved above, but ensure

                    await _context.SaveChangesAsync();

                    foreach (var groupId in groupIds.Where(id => id != request.TransApproverId))
                    {
                        var empName = _context
                            .CoreVEmployeeDetails.Where(n => n.EmplId == request.EmployeeId)
                            .Select(m => m.Employeename2)
                            .FirstOrDefault();
                        var groupApprover = allApprovers.First(a => a.Transapproverid == groupId);
                        groupApprover.Respond = 2;
                        groupApprover.Responddate = DateTime.UtcNow.AddHours(8);
                        // Use a standard message indicating auto-disapproval
                        groupApprover.Remarks =
                            $"Disapproved by: {empName ?? "No remarks provided"}";
                        await _context.SaveChangesAsync();
                    }

                    await _context
                        .EpeTransactionheaders.Where(h => h.Transactionid == request.TransactionId)
                        .ExecuteUpdateAsync(b =>
                            b.SetProperty(h => h.Status, "C")
                                .SetProperty(h => h.Finaldecision, "Disapproved")
                        );

                    await LogTransactionAsync(
                        transactionHeader.Transactionid,
                        transactionHeader.Policyid,
                        "CLOSE",
                        transactionHeader.Status,
                        "C",
                        userId
                    );

                    if (requester != null && !string.IsNullOrEmpty(requester.Emailadd))
                    {
                        try
                        {
                            var requesterUserId = 0;
                            var user = await _context.CoreVUsers.FirstOrDefaultAsync(u =>
                                u.EmplId == requester.EmplId
                            );
                            if (user != null)
                                requesterUserId = user.UserId;

                            await _emailService.SendStatusChangedEmailAsync(
                                requester.Emailadd,
                                transactionHeader.Transactionid,
                                transactionHeader.Transactionnumber,
                                "Pending",
                                "Disapproved",
                                "System",
                                requesterUserId
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Failed to send disapproved email to {Email}",
                                requester.Emailadd
                            );
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

        [HttpGet("attachments/{transactionId}")]
        public async Task<IActionResult> GetAttachments(int transactionId)
        {
            try
            {
                var transactionNumber = await _context
                    .EpeTransactionheaders.Where(a => a.Transactionid == transactionId)
                    .FirstAsync();

                var attachments = await _context
                    .EpeAttachments.Where(a => a.TaskId == transactionNumber.Transactionnumber)
                    .OrderBy(a => a.Createdate)
                    .Select(a => new
                    {
                        a.AttachId,
                        a.TaskId,
                        a.ReferenceType,
                        a.UploadFilename,
                        a.Newfilename,
                        a.Filetype,
                        a.Createdate,
                        a.Computerhost,
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = attachments });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        //[HttpPost("add-attachment/{transactionId}")]
        //public async Task<IActionResult> AddAttachment(int transactionId, [FromForm] UploadAttachmentRequest request)
        //{

        //    //_logger.LogInformation(request.Attachments.ToString());

        //    using var transaction = await _context.Database.BeginTransactionAsync();
        //    try
        //    {
        //        // Get transaction number
        //        var transactionHeader = await _context.EpeTransactionheaders
        //            .Where(t => t.Transactionid == transactionId && t.Isdelete == 0)
        //            .FirstOrDefaultAsync();

        //        if (transactionHeader == null)
        //        {
        //            return NotFound(new { success = false, message = "Transaction not found" });
        //        }

        //        if (request == null)
        //        {
        //            return BadRequest(new { success = false, message = "No attachments provided" });
        //        }

        //        foreach (var attachment in request)
        //        {
        //            var savedFileName = await _fileUploadService.SaveFile(attachment.File);
        //            var epeAttachment = new EpeAttachment
        //            {
        //                TaskId = transactionHeader.Transactionnumber,
        //                // ReferenceType = attachment.ReferenceType,
        //                UploadFilename = attachment.File.FileName,
        //                Newfilename = savedFileName,
        //                Filetype = Path.GetExtension(attachment.File.FileName).TrimStart('.'),
        //                Createdate = DateTime.UtcNow.AddHours(8),
        //                Computerhost = HttpContext.Request.Host.Host,
        //            };
        //            _context.EpeAttachments.Add(epeAttachment);
        //        }

        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        return Ok(new { success = true, message = "Attachments added successfully" });
        //    }
        //    catch (Exception e)
        //    {
        //        await transaction.RollbackAsync();
        //        return StatusCode(500, "Error: " + e.Message);
        //    }
        //}

        [HttpPost("add-attachment/{transactionId}")]
        public async Task<IActionResult> AddAttachment(
            int transactionId,
            [FromForm] UploadAttachmentRequest request
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var transactionHeader = await _context
                    .EpeTransactionheaders.Where(t =>
                        t.Transactionid == transactionId && t.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (transactionHeader == null)
                {
                    return NotFound(new { success = false, message = "Transaction not found" });
                }

                if (request?.Files == null || !request.Files.Any())
                {
                    return BadRequest(new { success = false, message = "No attachments provided" });
                }

                foreach (var file in request.Files)
                {
                    var savedFileName = await _fileUploadService.SaveFile(file);

                    var epeAttachment = new EpeAttachment
                    {
                        TaskId = transactionHeader.Transactionnumber,
                        UploadFilename = file.FileName,
                        Newfilename = savedFileName,
                        Filetype = Path.GetExtension(file.FileName).TrimStart('.'),
                        Createdate = DateTime.UtcNow.AddHours(8),
                        Computerhost = HttpContext.Request.Host.Host,
                    };

                    _context.EpeAttachments.Add(epeAttachment);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Attachments added successfully" });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = e.Message });
            }
        }

        [HttpPost("delete-attachment/{attachId}")]
        public async Task<IActionResult> DeleteAttachment(int attachId)
        {
            if (attachId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid request parameters" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var attachment = await _context
                    .EpeAttachments.Where(a => a.AttachId == attachId)
                    .FirstOrDefaultAsync();

                if (attachment == null)
                {
                    return NotFound(new { success = false, message = "Attachment not found" });
                }

                //// Verify ownership
                //var transactionHeader = await _context.EpeTransactionheaders
                //    .Where(t => t.Transactionid.ToString() == attachment.TaskId && t.Requestorid == request.Userid && t.Isdelete == 0)
                //    .FirstOrDefaultAsync();

                //if (transactionHeader == null)
                //{
                //    return StatusCode(403, new { success = false, message = "Unauthorized to delete this attachment" });
                //}

                //Build file path
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "attachments");
                var fullPath = Path.Combine(uploadsFolder, attachment.Newfilename ?? "");

                _logger.LogWarning("Full Path: " + fullPath);

                // Prevent directory traversal
                if (Path.GetFullPath(fullPath).StartsWith(Path.GetFullPath(uploadsFolder)))
                {
                    //Delete file if exists
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                    else
                    {
                        return StatusCode(
                            500,
                            new
                            {
                                success = false,
                                message = "An error occurred while deleting the attachment",
                            }
                        );
                    }
                }

                _context.EpeAttachments.Remove(attachment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Attachment deleted successfully" });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                // Log the error here if logging is available
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = "An error occurred while deleting the attachment",
                    }
                );
            }
        }

        [HttpPost("resend/{id}")]
        public async Task<IActionResult> ResendApprovalRequestEmail(
            int id,
            [FromBody] ResendRequest request
        )
        {
            try
            {
                // Get the approver record
                var approver = await _context
                    .EpeTransactionapprovers.Where(a =>
                        a.Transapproverid == request.TransApproverId && a.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (approver == null)
                {
                    return NotFound(new { success = false, message = "Approver not found" });
                }

                // Get the transaction header
                var transactionHeader = await _context
                    .EpeVTransactionHeaders.Where(u =>
                        u.TransactionId == approver.Transactionid && u.IsDelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (transactionHeader == null)
                {
                    return NotFound(
                        new { success = false, message = "Exemption request not found" }
                    );
                }

                // Get requester details
                var requester = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == transactionHeader.RequesterId)
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
                    var empDetail = await _context
                        .EpePolicyapprovers.Where(e => e.Id == int.Parse(approver.Approverid))
                        .FirstOrDefaultAsync();

                    approverEmail = empDetail?.Approveremail ?? "";
                    approverName = empDetail?.Approvername ?? "";
                }

                if (string.IsNullOrEmpty(approverEmail))
                {
                    return BadRequest(
                        new { success = false, message = "Approver email not found" }
                    );
                }

                // Send the approval request email
                try
                {
                    var transApproverRecord = await _context
                        .EpeTransactionapprovers.Where(a =>
                            a.Transactionid == transactionHeader.TransactionId
                            && a.Approverid == approver.Approverid.ToString()
                            && a.Isdelete == 0
                        )
                        .FirstOrDefaultAsync();
                    //var emplId = requester?.EmplId ?? "";
                    //var requesterName = await _context.CoreVUsers.Where(u => u.);

                    await _emailService.SendApprovalRequestEmailAsync(
                        approverEmail,
                        transactionHeader.TransactionId,
                        transactionHeader.TransactionNumber,
                        requester.Employeename2,
                        transactionHeader.Details ?? "No details provided",
                        int.Parse(requester?.EmplId),
                        transApproverRecord?.Transapproverid ?? 0
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to resend approval request email to {Email}",
                        approverEmail
                    );
                    return StatusCode(
                        500,
                        new { success = false, message = "Failed to send email: " + ex.Message }
                    );
                }

                // Update tracking fields
                approver.Issent = 1;
                approver.Sentcount = approver.Sentcount + 1;
                approver.Lastsentdate = DateTime.UtcNow.AddHours(8);

                // Set SentDate only on first send
                if (approver.Sentdate == null)
                {
                    approver.Sentdate = DateTime.UtcNow.AddHours(8);
                }

                await _context.SaveChangesAsync();

                return Ok(
                    new { success = true, message = "Approval request email resent successfully" }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("resend-main/{id}")]
        public async Task<IActionResult> ResendToMainApprover(
            int id,
            [FromBody] ResendRequest request
        )
        {
            try
            {
                var approver = await _context
                    .EpeTransactionapprovers.Where(a =>
                        a.Transapproverid == request.TransApproverId && a.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (approver == null)
                    return NotFound(new { success = false, message = "Approver not found" });

                var transactionHeader = await _context
                    .EpeVTransactionHeaders.Where(u =>
                        u.TransactionId == approver.Transactionid && u.IsDelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (transactionHeader == null)
                    return NotFound(new { success = false, message = "Transaction not found" });

                var requester = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == transactionHeader.RequesterId)
                    .FirstOrDefaultAsync();

                string approverEmail = "";
                string approverName = "";

                if (approver.Approvertype == "IM" && requester != null)
                {
                    approverEmail = requester.ImmediateEmail ?? "";
                    approverName = requester.ImmediateName ?? "";
                }
                else
                {
                    var policyApprover = await _context
                        .EpePolicyapprovers.Where(e => e.Id == int.Parse(approver.Approverid))
                        .FirstOrDefaultAsync();

                    approverEmail = policyApprover?.Approveremail ?? "";
                    approverName = policyApprover?.Approvername ?? "";
                }

                if (string.IsNullOrEmpty(approverEmail))
                    return BadRequest(
                        new { success = false, message = "Approver email not found" }
                    );

                var approverUserId = 0;
                if (!string.IsNullOrEmpty(approver.Approverid))
                {
                    var approverUser = await _context.CoreVUsers.FirstOrDefaultAsync(u =>
                        u.EmplId == approver.Approverid
                    );
                    if (approverUser != null)
                        approverUserId = approverUser.UserId;
                }

                var transApproverRecord = await _context
                    .EpeTransactionapprovers.Where(a =>
                        a.Transactionid == transactionHeader.TransactionId
                        && a.Approverid == approver.Approverid
                        && a.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                await _emailService.SendMainApproverReminderEmailAsync(
                    approverEmail,
                    approverName,
                    transactionHeader.TransactionId,
                    transactionHeader.TransactionNumber,
                    requester?.Employeename2 ?? transactionHeader.RequesterId,
                    transactionHeader.Details ?? "No details provided",
                    approverUserId,
                    transApproverRecord?.Transapproverid ?? 0
                );

                approver.Issent = 1;
                approver.Sentcount += 1;
                approver.Lastsentdate = DateTime.UtcNow.AddHours(8);
                if (approver.Sentdate == null)
                    approver.Sentdate = DateTime.UtcNow.AddHours(8);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Reminder sent to main approver" });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("resend-proxy/{id}")]
        public async Task<IActionResult> ResendToProxyApprover(
            int id,
            [FromBody] ResendRequest request
        )
        {
            try
            {
                var proxyApprover = await _context
                    .EpeTransactionapprovers.Where(a =>
                        a.Transapproverid == request.TransApproverId && a.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (proxyApprover == null)
                    return NotFound(new { success = false, message = "Proxy approver not found" });

                var transactionHeader = await _context
                    .EpeVTransactionHeaders.Where(u =>
                        u.TransactionId == proxyApprover.Transactionid && u.IsDelete == 0
                    )
                    .FirstOrDefaultAsync();

                if (transactionHeader == null)
                    return NotFound(new { success = false, message = "Transaction not found" });

                var requester = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == transactionHeader.RequesterId)
                    .FirstOrDefaultAsync();

                // Get the main approver in the same sort group
                var mainApproverRecord = await _context
                    .EpeTransactionapprovers.Where(a =>
                        a.Transactionid == proxyApprover.Transactionid
                        && a.Sort == proxyApprover.Sort
                        && a.Linesequence == 1
                        && a.Isdelete == 0
                    )
                    .FirstOrDefaultAsync();

                var policyProxyApprover = await _context
                    .EpePolicyapprovers.Where(e => e.Id == int.Parse(proxyApprover.Approverid))
                    .FirstOrDefaultAsync();

                string proxyEmail = policyProxyApprover?.Approveremail ?? "";
                string proxyName = policyProxyApprover?.Approvername ?? "";
                string mainApproverName = "Main Approver";

                if (mainApproverRecord != null)
                {
                    var policyMainApprover = await _context
                        .EpePolicyapprovers.Where(e =>
                            e.Id == int.Parse(mainApproverRecord.Approverid)
                        )
                        .FirstOrDefaultAsync();

                    if (mainApproverRecord.Approvertype == "IM" && requester != null)
                        mainApproverName = requester.ImmediateName ?? "Immediate Head";
                    else
                        mainApproverName = policyMainApprover?.Approvername ?? "Main Approver";
                }

                if (string.IsNullOrEmpty(proxyEmail))
                    return BadRequest(
                        new { success = false, message = "Proxy approver email not found" }
                    );

                var proxyUserId = 0;
                if (!string.IsNullOrEmpty(proxyApprover.Approverid))
                {
                    var proxyUser = await _context.CoreVUsers.FirstOrDefaultAsync(u =>
                        u.EmplId == proxyApprover.Approverid
                    );
                    if (proxyUser != null)
                        proxyUserId = proxyUser.UserId;
                }

                await _emailService.SendProxyApproverReminderEmailAsync(
                    proxyEmail,
                    proxyName,
                    mainApproverName,
                    transactionHeader.TransactionId,
                    transactionHeader.TransactionNumber,
                    requester?.Employeename2 ?? transactionHeader.RequesterId,
                    transactionHeader.Details ?? "No details provided",
                    proxyUserId,
                    proxyApprover.Transapproverid
                );

                proxyApprover.Issent = 1;
                proxyApprover.Sentcount += 1;
                proxyApprover.Lastsentdate = DateTime.UtcNow.AddHours(8);
                if (proxyApprover.Sentdate == null)
                    proxyApprover.Sentdate = DateTime.UtcNow.AddHours(8);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Reminder sent to proxy approver" });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("overall")]
        public async Task<IActionResult> GetExcemptionOverallList(
            [FromBody] ExemptionListRequest request,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var currentUserEmplId = request.UserId.ToString();

                var approverTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a => a.IsDelete == 0)
                    .Select(a => a.TransactionId)
                    .Distinct()
                    .ToListAsync();

                var query = _context.EpeVTransactionHeaders.Where(u =>
                    u.IsDelete == 0 && approverTransactionIds.Contains(u.TransactionId)
                );

                var searchTerm = request.Search ?? search;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchWords = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word)
                            || u.TransactionNumber.ToString().ToLower().Contains(word)
                            || u.RequesterName.ToString().Contains(word)
                            || u.FinalDecision.ToString().Contains(word)
                            || u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                if (!string.IsNullOrWhiteSpace(request.PolicyNames))
                {
                    var selectedIds = request
                        .PolicyNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => int.TryParse(p, out _))
                        .Select(int.Parse)
                        .ToList();

                    if (selectedIds.Any())
                    {
                        query = query.Where(u => selectedIds.Contains(u.PolicyId));
                    }
                }

                query = request.SortField switch
                {
                    "transactionNumber" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.TransactionNumber)
                        : query.OrderBy(u => u.TransactionNumber),
                    "policyName" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PolicyName)
                        : query.OrderBy(u => u.PolicyName),
                    "createdDate" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.CreatedDate)
                        : query.OrderBy(u => u.CreatedDate),
                    _ => query.OrderByDescending(u => u.CreatedDate),
                };

                var totalCount = await query.CountAsync();

                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        TransactionId = h.TransactionId,
                        TransactionNumber = h.TransactionNumber,
                        PolicyName = h.PolicyName,
                        Status = h.Status,
                        RequesterName = h.RequesterName,
                        CreatedDate = h.CreatedDate,
                        RequesterId = h.RequesterId,
                        FinalDecision = h.FinalDecision,

                        CanApprove = approverTransactionIds.Contains(h.TransactionId),
                    })
                    .ToListAsync();

                return Ok(
                    new
                    {
                        success = true,
                        items = items,
                        totalCount,
                        page,
                        pageSize,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("overall/export")]
        public async Task<IActionResult> ExportOverallExemptions(
            [FromBody] ExemptionListRequest request
        )
        {
            try
            {
                var currentUserEmplId = request.UserId.ToString();

                var approverTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a => a.IsDelete == 0)
                    .Select(a => a.TransactionId)
                    .Distinct()
                    .ToListAsync();

                var query = _context.EpeVTransactionHeaders.Where(u =>
                    u.IsDelete == 0 && approverTransactionIds.Contains(u.TransactionId)
                );

                var searchTerm = request.Search;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchWords = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word)
                            || u.TransactionNumber.ToString().ToLower().Contains(word)
                            || u.RequesterName.ToString().Contains(word)
                            || u.FinalDecision.ToString().Contains(word)
                            || u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                if (!string.IsNullOrWhiteSpace(request.PolicyNames))
                {
                    var selectedIds = request
                        .PolicyNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => int.TryParse(p, out _))
                        .Select(int.Parse)
                        .ToList();

                    if (selectedIds.Any())
                    {
                        query = query.Where(u => selectedIds.Contains(u.PolicyId));
                    }
                }

                var items = await query
                    .Select(h => new
                    {
                        TransactionId = h.TransactionId,
                        TransactionNumber = h.TransactionNumber,
                        PolicyName = h.PolicyName,
                        Status = h.Status,
                        RequesterName = h.RequesterName,
                        CreatedDate = h.CreatedDate.ToString("MMM d, yyyy h:mm tt"),
                        FinalDecision = h.FinalDecision,
                    })
                    .ToListAsync();

                return Ok(new { success = true, items });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("policy-transaction")]
        public async Task<IActionResult> GetPolicyTransactionLogs(
            [FromBody] ExemptionListRequest request,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var currentUserEmplId = request.UserId.ToString();

                var approverTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .Distinct()
                    .ToListAsync();

                var query = _context.EpeVTransactionHeaders.Where(u =>
                    u.IsDelete == 0
                    && (
                        u.RequesterId == currentUserEmplId
                        || approverTransactionIds.Contains(u.TransactionId)
                    )
                );

                var searchTerm = request.Search ?? search;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchWords = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var predicate = PredicateBuilder.New<EpeVTransactionHeader>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.TransactionId.ToString().Contains(word)
                            || u.TransactionNumber.ToString().ToLower().Contains(word)
                            || u.RequesterName.ToString().Contains(word)
                            || u.FinalDecision.ToString().Contains(word)
                            || u.PolicyName.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                if (!string.IsNullOrWhiteSpace(request.PolicyNames))
                {
                    var selectedIds = request
                        .PolicyNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => int.TryParse(p, out _))
                        .Select(int.Parse)
                        .ToList();

                    if (selectedIds.Any())
                    {
                        query = query.Where(u => selectedIds.Contains(u.PolicyId));
                    }
                }

                if (
                    !string.IsNullOrWhiteSpace(request.StartDate)
                    && DateTime.TryParse(request.StartDate, out var startDate)
                )
                {
                    query = query.Where(u => u.CreatedDate >= startDate);
                }
                if (
                    !string.IsNullOrWhiteSpace(request.EndDate)
                    && DateTime.TryParse(request.EndDate, out var endDate)
                )
                {
                    query = query.Where(u => u.CreatedDate <= endDate);
                }

                query = request.SortField switch
                {
                    "transactionNumber" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.TransactionNumber)
                        : query.OrderBy(u => u.TransactionNumber),
                    "policyName" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PolicyName)
                        : query.OrderBy(u => u.PolicyName),
                    "createdDate" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.CreatedDate)
                        : query.OrderBy(u => u.CreatedDate),
                    "requesterName" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.RequesterName)
                        : query.OrderBy(u => u.RequesterName),
                    _ => query.OrderByDescending(u => u.CreatedDate),
                };

                var totalCount = await query.CountAsync();

                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;

                var createdBy = _context
                    .CoreVEmployeeDetails.Where(u =>
                        u.EmplId == query.Select(q => q.CreatedBy).FirstOrDefault()
                    )
                    .Select(u => u.Employeename2)
                    .FirstOrDefault();

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        TransactionId = h.TransactionId,
                        TransactionNumber = h.TransactionNumber,
                        PolicyName = h.PolicyName,
                        Status = h.Status,
                        RequesterName = h.RequesterName,
                        CreatedDate = h.CreatedDate,
                        RequesterId = h.RequesterId,
                        FinalDecision = h.FinalDecision,
                        CreatedBy = createdBy,
                        ModifiedBy = h.ModifiedBy,
                        ModifiedDate = h.ModifiedDate,
                        Workstation = h.Workstation,
                        IpAddress = h.IpAddress,
                        MacAddress = h.MacAddress,

                        CanApprove = approverTransactionIds.Contains(h.TransactionId),
                    })
                    .ToListAsync();

                return Ok(
                    new
                    {
                        success = true,
                        items = items,
                        totalCount,
                        page,
                        pageSize,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("policies")]
        public async Task<IActionResult> GetPoliciesWithRequests(
            [FromBody] ExemptionListRequest request
        )
        {
            try
            {
                var currentUserEmplId = request.UserId.ToString();

                var approverTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .Distinct()
                    .ToListAsync();

                var policies = await _context
                    .EpeVTransactionHeaders.Where(u =>
                        u.FinalDecision != "Draft"
                        && u.IsDelete == 0
                        && !string.IsNullOrEmpty(u.PolicyName)
                        && u.PolicyName != "-"
                        && (
                            u.RequesterId == currentUserEmplId
                            || approverTransactionIds.Contains(u.TransactionId)
                        )
                    )
                    .GroupBy(u => new { u.PolicyId, u.PolicyName })
                    .Select(g => new { id = g.Key.PolicyId, label = g.Key.PolicyName })
                    .OrderBy(p => p.label)
                    .ToListAsync();

                return Ok(new { success = true, items = policies });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        private async Task LogTransactionAsync(
            long transactionId,
            int policyId,
            string transactionType,
            string? previousStatus,
            string? newStatus,
            int performedBy,
            string? remarks = null,
            string? reason = null,
            string? metadata = null
        )
        {
            try
            {
                var ipAddress =
                    HttpContext
                        .Request.Headers["X-Forwarded-For"]
                        .FirstOrDefault()
                        ?.Split(',')
                        .FirstOrDefault()
                        ?.Trim()
                    ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "Unknown";

                var macAddress =
                    System
                        .Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                        .FirstOrDefault(n =>
                            n.OperationalStatus
                                == System.Net.NetworkInformation.OperationalStatus.Up
                            && n.NetworkInterfaceType
                                != System.Net.NetworkInformation.NetworkInterfaceType.Loopback
                        )
                        ?.GetPhysicalAddress()
                        .ToString()
                    ?? "Unknown";

                var workstation =
                    HttpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
                    ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "Unknown";

                var log = new EpeTransactionlog
                {
                    TransactionId = transactionId,
                    PolicyId = policyId,
                    TransactionType = transactionType,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    PerformedBy = int.Parse(performedBy.ToString()),
                    PerformedAt = DateTime.UtcNow.AddHours(8),
                    Remarks = remarks,
                    Reason = reason,
                    Metadata = metadata,
                    Workstation = workstation,
                    IpAddress = ipAddress,
                    MacAddress = macAddress,
                };

                _context.EpeTransactionlogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to log transaction {TransactionId} type {TransactionType}",
                    transactionId,
                    transactionType
                );
            }
        }

        [HttpPost("transaction-logs")]
        public async Task<IActionResult> GetTransactionLogsList(
            [FromBody] ExemptionListRequest request,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var currentUserEmplId = request.UserId.ToString();

                var approverTransactionIds = await _context
                    .EpeVTransactionApprovers.Where(a =>
                        a.EmployeeId == currentUserEmplId && a.IsDelete == 0
                    )
                    .Select(a => a.TransactionId)
                    .Distinct()
                    .ToListAsync();

                var query =
                    from log in _context.EpeTransactionlogs
                    join header in _context.EpeVTransactionHeaders
                        on log.TransactionId equals header.TransactionId
                    where
                        header.IsDelete == 0
                        && (
                            header.RequesterId == currentUserEmplId
                            || approverTransactionIds.Contains(header.TransactionId)
                        )
                    select new
                    {
                        log.Id,
                        log.TransactionId,
                        TransactionNumber = header.TransactionNumber,
                        PolicyName = header.PolicyName,
                        log.TransactionType,
                        log.PreviousStatus,
                        log.NewStatus,
                        PerformedBy = _context
                            .CoreVUsers.Where(e => e.UserId == log.PerformedBy)
                            .Select(a => a.Employeename)
                            .FirstOrDefault(),
                        log.PerformedAt,
                        log.Remarks,
                        log.Reason,
                        log.Metadata,
                        log.Workstation,
                        log.IpAddress,
                        log.MacAddress,
                        RequesterId = header.RequesterId,
                        RequesterName = header.RequesterName,
                        header.Status,
                        header.FinalDecision,
                    };

                var searchTerm = request.Search ?? search;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchWords = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var word in searchWords)
                    {
                        query = query.Where(u =>
                            u.TransactionNumber.ToLower().Contains(word)
                            || u.PolicyName.ToLower().Contains(word)
                            || u.TransactionType.ToLower().Contains(word)
                            || u.RequesterName.ToLower().Contains(word)
                            || (u.Remarks != null && u.Remarks.ToLower().Contains(word))
                            || (u.Reason != null && u.Reason.ToLower().Contains(word))
                        );
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.PolicyNames))
                {
                    var selectedIds = request
                        .PolicyNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => int.TryParse(p, out _))
                        .Select(int.Parse)
                        .ToList();

                    if (selectedIds.Any())
                    {
                        var selectedPolicyNames = _context
                            .EpePolicymasters.Where(p =>
                                selectedIds.Contains(p.Policyid) && p.Isdelete == 0
                            )
                            .Select(p => p.Description)
                            .ToList();

                        query = query.Where(u => selectedPolicyNames.Contains(u.PolicyName));
                    }
                }

                if (
                    !string.IsNullOrWhiteSpace(request.StartDate)
                    && DateTime.TryParse(request.StartDate, out var startDate)
                )
                {
                    query = query.Where(u => u.PerformedAt >= startDate);
                }
                if (
                    !string.IsNullOrWhiteSpace(request.EndDate)
                    && DateTime.TryParse(request.EndDate, out var endDate)
                )
                {
                    query = query.Where(u => u.PerformedAt <= endDate.Date.AddDays(1));
                }

                query = request.SortField switch
                {
                    "transactionNumber" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.TransactionNumber)
                        : query.OrderBy(u => u.TransactionNumber),
                    "policyName" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PolicyName)
                        : query.OrderBy(u => u.PolicyName),
                    "transactionType" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.TransactionType)
                        : query.OrderBy(u => u.TransactionType),
                    "performedAt" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PerformedAt)
                        : query.OrderBy(u => u.PerformedAt),
                    "performedBy" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.PerformedBy)
                        : query.OrderBy(u => u.PerformedBy),
                    _ => query.OrderByDescending(u => u.PerformedAt),
                };

                var totalCount = await query.CountAsync();

                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;

                var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                return Ok(
                    new
                    {
                        success = true,
                        items = items,
                        totalCount,
                        page,
                        pageSize,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpGet("transaction-logs/all")]
        public async Task<IActionResult> GetAllTransactionLogs()
        {
            try
            {
                var logs = await _context
                    .EpeTransactionlogs.OrderByDescending(l => l.PerformedAt)
                    .ToListAsync();

                return Ok(new { success = true, items = logs });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpGet("transaction-log/{id}")]
        public async Task<IActionResult> GetTransactionLogById(long id)
        {
            try
            {
                var log = await _context.EpeTransactionlogs.FirstOrDefaultAsync(l => l.Id == id);

                if (log == null)
                {
                    return NotFound(new { success = false, message = "Transaction log not found" });
                }

                return Ok(new { success = true, item = log });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpGet("transaction-logs/{transactionId}")]
        public async Task<IActionResult> GetTransactionLogs(long transactionId)
        {
            try
            {
                var logs = await _context
                    .EpeTransactionlogs.Where(l => l.TransactionId == transactionId)
                    .OrderByDescending(l => l.PerformedAt)
                    .ToListAsync();

                return Ok(new { success = true, items = logs });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        //Create a new transaction log entry
        //[HttpPost("create-transaction-logs")]
        //public async Task<IActionResult> CreateTransactionLog([FromBody] TransactionLogRequest request)
        //{
        //    try
        //    {
        //        await LogTransactionAsync(
        //            request.TransactionId,
        //            request.PolicyId,
        //            request.TransactionType,
        //            request.PreviousStatus?.ToString(),
        //            request.NewStatus?.ToString(),
        //            request.PerformedBy,
        //            request.Remarks,
        //            request.Reason,
        //            request.Metadata
        //        );

        //        return Ok(new { success = true });
        //    }
        //    catch (Exception e)
        //    {
        //        return StatusCode(500, "Error: " + e.Message);
        //    }
        //}

        public class TransactionLogRequest
        {
            public long TransactionId { get; set; }
            public int PolicyId { get; set; }
            public string? TransactionType { get; set; }
            public int? PreviousStatus { get; set; }
            public int? NewStatus { get; set; }
            public int PerformedBy { get; set; }
            public string? Remarks { get; set; }
            public string? Reason { get; set; }
            public string? Metadata { get; set; }
        }
    }
}

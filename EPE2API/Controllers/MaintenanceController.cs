using Azure.Core;
using EPEAPI.Dto;
using EPEAPI.Models;
using LinqKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly EpeDbContext _context;

        public MaintenanceController(EpeDbContext contextdb)
        {
            _context = contextdb;
        }

        public class PolicyListRequest
        {
            public int UserId { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public string? Search { get; set; }
            public string? SortField { get; set; }
            public string? SortDirection { get; set; }
        }

        [HttpGet("policy")]
        public async Task<IActionResult> GetPolicy()
        {
            try
            {
                var policy = await _context
                    .EpePolicymasters.Where(p => p.Isdelete == 0)
                    .ToListAsync();
                if (policy == null || policy.Count == 0)
                {
                    return NotFound(new { success = false, message = "No policy found" });
                }

                return Ok(new { success = true, items = policy });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("policy")]
        public async Task<IActionResult> PostPolicy(
            [FromBody] PolicyListRequest request,
            [FromQuery] string? search = null
        )
        {
            try
            {
                var currentUserEmplId = request.UserId;

                var query = _context.EpePolicymasters.Where(p => p.Isdelete == 0);

                var searchTerm = request.Search ?? search;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var words = searchTerm
                        .ToLower()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var predicate = PredicateBuilder.New<EpePolicymaster>(true);
                    foreach (var word in words)
                    {
                        var w = word;
                        predicate = predicate.And(u =>
                            u.Policyid.ToString().Contains(w)
                            || u.Shortname.ToLower().Contains(w)
                            || u.Description.ToLower().Contains(w)
                            || u.Status.ToLower().Contains(w)
                        );
                    }
                    query = query.Where(predicate);
                }

                query = request.SortField switch
                {
                    "Policyid" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.Policyid)
                        : query.OrderBy(u => u.Policyid),
                    "Shortname" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.Shortname)
                        : query.OrderBy(u => u.Shortname),
                    "Description" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.Description)
                        : query.OrderBy(u => u.Description),
                    "Status" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.Status)
                        : query.OrderBy(u => u.Status),
                    _ => query.OrderByDescending(u => u.Createddate),
                };

                var totalCount = await query.CountAsync();

                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;

                var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

                return Ok(
                    new
                    {
                        success = true,
                        items,
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

        [HttpGet("policy/{id}")]
        public async Task<IActionResult> GetPolicyId(int id)
        {
            try
            {
                var policy = await _context
                    .EpePolicymasters.Where(p => p.Policyid == id && p.Isdelete == 0)
                    .FirstAsync();
                if (policy == null)
                {
                    return NotFound(new { success = false, message = "Policy Not Found" });
                }

                return Ok(new { success = true, items = policy });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("policy/create")]
        public async Task<IActionResult> CreatePolicy([FromBody] PolicyCreateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }

            if (string.IsNullOrWhiteSpace(request.Shortname))
            {
                return BadRequest(new { success = false, message = "Shortname is required" });
            }

            var existingShortname = await _context.EpePolicymasters
                .AnyAsync(p => p.Shortname == request.Shortname && p.Isdelete == 0);

            if (existingShortname)
            {
                return Conflict(new { success = false, message = "Shortname already exists" });
            }

            try
            {
                var policy = new EpePolicymaster
                {
                    Description = request.Description,
                    Shortname = request.Shortname,
                    Status = request.Status,
                    Createdby = request.UserId,
                    Createddate = DateTime.UtcNow.AddHours(8),
                    Isdelete = 0,
                };

                _context.EpePolicymasters.Add(policy);
                await _context.SaveChangesAsync();

                return Ok(
                    new
                    {
                        success = true,
                        message = "Policy created successfully",
                        data = policy,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("policy/update/{id}")]
        public async Task<IActionResult> UpdatePolicy(
            int id,
            [FromBody] PolicyUpdateRequest request
        )
        {
            try
            {
                var policy = await _context.EpePolicymasters.FindAsync(id);
                if (policy == null || policy.Isdelete == 1)
                {
                    return NotFound(new { success = false, message = "Policy not found" });
                }

                if (
                    string.IsNullOrWhiteSpace(request.Description)
                    || string.IsNullOrWhiteSpace(request.Shortname)
                )
                {
                    return BadRequest(
                        new { success = false, message = "Description and Shortname are required" }
                    );
                }

                var existingShortname = await _context.EpePolicymasters
                    .AnyAsync(p => p.Shortname == request.Shortname && p.Isdelete == 0 && p.Policyid != id);

                if (existingShortname)
                {
                    return Conflict(new { success = false, message = "Shortname already exists" });
                }

                policy.Description = request.Description;
                policy.Shortname = request.Shortname;
                policy.Status = request.Status;
                policy.Modifieddate = DateTime.Now;
                policy.Modifiedby = request.UserId;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, policy });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("policy/delete/{id}")]
        public async Task<IActionResult> DeletePolicy(
            int id,
            [FromBody] PolicyDeleteRequest request
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingItem = await _context
                    .EpePolicymasters.Where(u => u.Policyid == id && u.Isdelete == 0)
                    .FirstOrDefaultAsync();

                if (existingItem == null)
                {
                    return NotFound(new { success = false, message = "Policy not found" });
                }

                existingItem.Isdelete = 1;
                existingItem.Modifiedby = request.UserId;
                existingItem.Modifieddate = DateTime.UtcNow.AddHours(8);
                await transaction.CommitAsync();
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Policy deleted successfully" });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        // Policy Approver Maintenance

        [HttpPost("policy/{policyId}/approvers/create")]
        public async Task<IActionResult> CreateApprover(
            int policyId,
            [FromBody] CreateApproverRequest request
        )
        {
            try
            {
                var approver = new EpePolicyapprover
                {
                    Policyid = policyId,
                    Approverid = request.Approverid,
                    Approvername = request.Approvername,
                    Approveremail = request.Approveremail,
                    Approvertype = request.Approvertype,
                    Sort = request.Sort,
                    Linesequence = request.Linesequence,
                    Status = "A",
                    Isdelete = 0,
                    Createdby = request.CreatedBy,
                    Createddate = DateTime.UtcNow.AddHours(8),
                };

                _context.EpePolicyapprovers.Add(approver);
                await _context.SaveChangesAsync();

                return Ok(
                    new
                    {
                        success = true,
                        message = "Approver added",
                        item = approver,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("policy/{policyId}/approvers")]
        public async Task<IActionResult> GetPolicyApprover(int policyId, ApproverRequest request)
        {
            try
            {
                var userId = request.UserId;

                var empId = await _context
                    .CoreVUsers.Where(u => u.UserId == userId)
                    .FirstOrDefaultAsync();

                if (empId == null)
                {
                    return BadRequest(
                        new
                        {
                            success = false,
                            message = "User not found",
                            userId = request,
                        }
                    );
                }

                var items = await _context
                    .EpePolicyapprovers.Where(a => a.Policyid == policyId && a.Isdelete == 0)
                    .OrderBy(u => u.Linesequence)
                    .ThenBy(u => u.Sort)
                    .ToListAsync();

                var imHead = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == empId.EmplId)
                    .Select(e => new
                    {
                        e.ImmediateId,
                        e.ImmediateName,
                        e.ImmediateEmail,
                    })
                    .FirstOrDefaultAsync();

                var itemsWithRole = items
                    .Select(a => new
                    {
                        a.Id,
                        a.Policyid,
                        a.Approverid,
                        Approvername = a.Approvername,
                        Approveremail = a.Approveremail,
                        a.Approvertype,
                        a.Sort,
                        a.Linesequence,
                        a.Status,
                        a.Isdelete,
                        IsMainApprover = a.Linesequence == 1,
                        ApproverRole = a.Linesequence == 1 ? "Main" : "Proxy",
                    })
                    .ToList();

                //    Approvername = a.Approvertype == "IM" && imHead != null ? imHead.ImmediateName : a.Approvername,
                //    Approveremail = a.Approvertype == "IM" && imHead != null ? imHead.ImmediateEmail : a.Approveremail,

                return Ok(new { success = true, items = itemsWithRole });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("policy/{policyId}/view/approvers")]
        public async Task<IActionResult> ViewPolicyApprover(int policyId, ApproverRequest request)
        {
            try
            {
                var userId = request.UserId;

                var empId = await _context
                    .CoreVUsers.Where(u => u.UserId == userId)
                    .FirstOrDefaultAsync();

                if (empId == null)
                {
                    return BadRequest(
                        new
                        {
                            success = false,
                            message = "User not found",
                            userId = request,
                        }
                    );
                }

                var items = await _context
                    .EpePolicyapprovers.Where(a => a.Policyid == policyId && a.Isdelete == 0)
                    .OrderBy(u => u.Linesequence)
                    .ThenBy(u => u.Sort)
                    .ToListAsync();

                var imHead = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == empId.EmplId)
                    .Select(e => new
                    {
                        e.ImmediateId,
                        e.ImmediateName,
                        e.ImmediateEmail,
                    })
                    .FirstOrDefaultAsync();

                var itemsWithRole = items
                    .Select(a => new
                    {
                        a.Id,
                        a.Policyid,
                        a.Approverid,
                        Approvername = a.Approvertype == "IM" && imHead != null
                            ? imHead.ImmediateName
                            : a.Approvername,
                        Approveremail = a.Approvertype == "IM" && imHead != null
                            ? imHead.ImmediateEmail
                            : a.Approveremail,
                        a.Approvertype,
                        a.Sort,
                        a.Linesequence,
                        a.Status,
                        a.Isdelete,
                        IsMainApprover = a.Linesequence == 1,
                        ApproverRole = a.Linesequence == 1 ? "Main" : "Proxy",
                    })
                    .ToList();

                //    Approvername = a.Approvertype == "IM" && imHead != null ? imHead.ImmediateName : a.Approvername,
                //    Approveremail = a.Approvertype == "IM" && imHead != null ? imHead.ImmediateEmail : a.Approveremail,

                return Ok(new { success = true, items = itemsWithRole });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpPost("create/policy/{policyId}/approvers")]
        public async Task<IActionResult> GetPolicyApproverCreate(
            int policyId,
            ApproverRequest request
        )
        {
            try
            {
                var userId = request.UserId;

                var empId = await _context
                    .CoreVUsers.Where(u => u.UserId == userId)
                    .FirstOrDefaultAsync();

                if (empId == null)
                {
                    return BadRequest(
                        new
                        {
                            success = false,
                            message = "User not found",
                            userId = request,
                        }
                    );
                }

                var items = await _context
                    .EpePolicyapprovers.Where(a => a.Policyid == policyId && a.Isdelete == 0)
                    .OrderBy(u => u.Linesequence)
                    .ThenBy(u => u.Sort)
                    .ToListAsync();

                var imHead = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == empId.EmplId)
                    .Select(e => new
                    {
                        e.ImmediateId,
                        e.ImmediateName,
                        e.ImmediateEmail,
                    })
                    .FirstOrDefaultAsync();

                var itemsWithRole = items
                    .Select(a => new
                    {
                        a.Id,
                        a.Policyid,
                        a.Approverid,
                        Approvername = a.Approvertype == "IM" && imHead != null
                            ? imHead.ImmediateName
                            : a.Approvername,
                        Approveremail = a.Approvertype == "IM" && imHead != null
                            ? imHead.ImmediateEmail
                            : a.Approveremail,
                        a.Approvertype,
                        a.Sort,
                        a.Linesequence,
                        a.Status,
                        a.Isdelete,
                        IsMainApprover = a.Linesequence == 1,
                        ApproverRole = a.Linesequence == 1 ? "Main" : "Proxy",
                    })
                    .ToList();

                //    Approvername = a.Approvertype == "IM" && imHead != null ? imHead.ImmediateName : a.Approvername,
                //    Approveremail = a.Approvertype == "IM" && imHead != null ? imHead.ImmediateEmail : a.Approveremail,

                return Ok(new { success = true, items = itemsWithRole });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpGet("employees/immediate-head")]
        public async Task<IActionResult> GetImmediateHead([FromQuery] int userId)
        {
            try
            {
                var user = await _context
                    .CoreVUsers.Where(u => u.UserId == userId)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var empDetails = await _context
                    .CoreVEmployeeDetails.Where(e => e.EmplId == user.EmplId)
                    .Select(e => new
                    {
                        immediateId = e.ImmediateId,
                        immediateName = e.ImmediateName,
                        immediateEmail = e.ImmediateEmail,
                    })
                    .FirstOrDefaultAsync();

                if (empDetails == null)
                    return NotFound(new { success = false, message = "Employee not found" });

                return Ok(new { success = true, immediateId = empDetails.immediateId });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }

        [HttpGet("employees/search")]
        public async Task<IActionResult> SearchEmployees([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<object>());

            q = q.Trim();

            var searchWords = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var query = _context.CoreVEmployeeDetails.Where(e => e.Typedescription == "ACTIVE").AsNoTracking(); // IMPORTANT for views / keyless entities

            foreach (var word in searchWords)
            {
                var temp = word;

                query = query.Where(e =>
                    EF.Functions.Like(e.Fname ?? "", $"%{temp}%")
                    || EF.Functions.Like(e.Lname ?? "", $"%{temp}%")
                    || EF.Functions.Like(e.Mname ?? "", $"%{temp}%")
                    || EF.Functions.Like(e.Emailadd ?? "", $"%{temp}%")
                );
            }

            var result = query
                .OrderBy(e => e.Lname)
                .Take(20)
                .Select(e => new
                {
                    id = e.EmplId,
                    label = e.Employeename2,
                    email = e.Emailadd,
                })
                .ToList();

            return Ok(result);
        }

        [HttpPost("policy/{policyId}/approvers/delete/{approverId}")]
        public async Task<IActionResult> DeletePolicyApprover(int policyId, int approverId)
        {
            if (policyId <= 0)
            {
                return NotFound(new { success = false, message = "Policy not found" });
            }

            if (approverId <= 0)
            {
                return NotFound(new { success = false, message = "Approver not found" });
            }

            var approver = await _context.EpePolicyapprovers.FirstOrDefaultAsync(a =>
                a.Policyid == policyId && a.Id == approverId && a.Isdelete == 0
            );

            if (approver == null)
            {
                return NotFound(new { success = false, message = "Approver not found" });
            }

            _context.EpePolicyapprovers.Remove(approver);   
            // approver.Isdelete = 1;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Approver deleted" });
        }

        [HttpPost("policy/{policyId}/approvers/reorder")]
        public async Task<IActionResult> ReorderApprovers(
            int policyId,
            [FromBody] List<ReorderApproverRequest> requests
        )
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest(new { success = false, message = "No reorder data provided" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var request in requests)
                {
                    var approver = await _context.EpePolicyapprovers.FirstOrDefaultAsync(a =>
                        a.Id == request.Id && a.Policyid == policyId && a.Isdelete == 0
                    );

                    if (approver == null)
                    {
                        return NotFound(
                            new { success = false, message = $"Approver {request.Id} not found" }
                        );
                    }

                    approver.Sort = request.Sort;
                    approver.Linesequence = request.Linesequence;
                    approver.Modifieddate = DateTime.UtcNow.AddHours(8);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Approvers reordered successfully" });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Error: " + e.Message });
            }
        }

        [HttpPost("policy/{policyId}/approvers/update/{approverId}")]
        public async Task<IActionResult> UpdateApprover(
            int policyId,
            int approverId,
            [FromBody] UpdateApproverRequest request
        )
        {
            if (policyId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid policy ID" });
            }

            if (approverId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid approver ID" });
            }

            var approver = await _context.EpePolicyapprovers.FirstOrDefaultAsync(a =>
                a.Policyid == policyId && a.Id == approverId
            );

            if (approver == null)
            {
                return NotFound(new { success = false, message = "Approver not found" });
            }

            approver.Isdelete = request.Isdelete;
            approver.Sort = request.Sort;
            approver.Linesequence = request.Linesequence;
            approver.Modifieddate = DateTime.UtcNow.AddHours(8);
            approver.Modifiedby = request.ModifiedBy;

            await _context.SaveChangesAsync();

            return Ok(
                new
                {
                    success = true,
                    message = "Approver updated successfully",
                    item = approver,
                }
            );
        }
    }
}

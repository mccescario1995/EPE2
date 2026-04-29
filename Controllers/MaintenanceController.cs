using EPEAPI.Dto;
using EPEAPI.Models;
using LinqKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet("policy")]
        public async Task<IActionResult> GetPolicy()
        {
            try
            {
                var policy = await _context.EpePolicymasters.Where(p => p.Isdelete == 0).ToListAsync();
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

        [HttpGet("policy/{id}")]
        public async Task<IActionResult> GetPolicyId(int id)
        {
            try
            {
                var policy = await _context.EpePolicymasters.Where(p => p.Policyid == id && p.Isdelete == 0).FirstAsync();
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

            try
            {
                var policy = new EpePolicymaster
                {
                    Description = request.Description,
                    Shortname = request.Shortname,
                    Status = request.Status,
                    Createdby = request.UserId,
                    Createddate = DateTime.UtcNow.AddHours(8),
                    Isdelete = 0
                };

                _context.EpePolicymasters.Add(policy);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Policy created successfully", data = policy });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }

        [HttpPost("policy/update/{id}")]
        public async Task<IActionResult> UpdatePolicy(int id, [FromBody] PolicyUpdateRequest request)
        {
            try
            {
                var policy = await _context.EpePolicymasters.FindAsync(id);
                if (policy == null || policy.Isdelete == 1)
                {
                    return NotFound(new { success = false, message = "Policy not found" });
                }

                if (string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Shortname))
                {
                    return BadRequest(new { success = false, message = "Description and Shortname are required" });
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
        public async Task<IActionResult> DeletePolicy(int id, [FromBody] PolicyDeleteRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingItem = await _context.EpePolicymasters
                   .Where(u => u.Policyid == id && u.Isdelete == 0)
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
        [HttpGet("policy/{policyId}/approvers")]
        public async Task<IActionResult> GetPolicyApprover(int policyId)
        {
            try
            {
                var items = await _context.EpePolicyapprovers
                    .Where(a => a.Policyid == policyId && a.Isdelete == 0)
                    .OrderBy(u => u.Linesequence)
                    .ThenBy(u => u.Sort)
                    .ToListAsync();

                var itemsWithRole = items.Select(a => new
                {
                    a.Id,
                    a.Policyid,
                    a.Approverid,
                    a.Approvername,
                    a.Approveremail,
                    a.Approvertype,
                    a.Sort,
                    a.Linesequence,
                    a.Status,
                    a.Isdelete,
                    IsMainApprover = a.Linesequence == 1,
                    ApproverRole = a.Linesequence == 1 ? "Main" : "Proxy"
                }).ToList();

                return Ok(new { success = true, items = itemsWithRole });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }
    }
}
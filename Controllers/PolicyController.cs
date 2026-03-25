using EPEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinqKit;

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicyController : ControllerBase
    {
        private readonly EpeDbContext _context;
        public PolicyController(EpeDbContext contextdb)
        {
            _context = contextdb;
        }

        [HttpPost]
        public async Task<IActionResult> GetPolicyList()
        {
            try
            {

                var query = _context.EpePolicymasters.Where(u => u.Isdelete == 0);

                var items = await query
                    .OrderByDescending(u => u.Createddate)
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

        [HttpPost("approvers/{policyId}")]
        public async Task<IActionResult> GetPolicyApprover(int policyId, ApproverRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = request.UserId;

                var empId = await _context.CoreVUsers
                        .Where(u => u.UserId == userId)
                        .FirstOrDefaultAsync();

                if (empId == null)
                {
                    return BadRequest(new { success = false, message = "User not found", userId = request });
                }

                var items = await _context.EpePolicyapprovers.Where(a => a.Policyid == policyId && a.Isdelete == 0).OrderBy(u => u.Linesequence).ThenBy(u => u.Sort).ToListAsync();

                var imHead = await _context.CoreVEmployeeDetails
                        .Where(e => e.EmplId == empId.EmplId)
                        .Select(e => new
                        {
                            e.ImmediateId,
                            e.ImmediateName,
                            e.ImmediateEmail
                        })
                        .FirstOrDefaultAsync();

                if (items.Any() && imHead != null)
                {
                    items[0].Approverid = imHead.ImmediateId;
                    items[0].Approvername = imHead.ImmediateName;
                    items[0].Approveremail = imHead.ImmediateEmail;
                }

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
                    a.Createdby,
                    a.Createddate,
                    a.Modifiedby,
                    a.Modifieddate,
                    IsMainApprover = a.Linesequence == 1,
                    ApproverRole = a.Linesequence == 1 ? "Main" : "Proxy"
                }).ToList();

                return Ok(new
                {
                    success = true,
                    items = itemsWithRole,
                });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error" + e.Message);
            }

        }
    }
}
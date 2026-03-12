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
        public async Task<IActionResult> GetPolicyApprover([FromQuery] int policyId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var query = _context.EpePolicyapprovers.Where(a => a.Policyid == policyId).Where(u => u.Isdelete == 0);

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
                await transaction.RollbackAsync();
                return StatusCode(500, "Error" + e.Message);
            }

        }
    }
}
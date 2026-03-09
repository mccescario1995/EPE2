using EPEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinqKit;

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly EpeDbContext _context;
        public AuthenticateController(EpeDbContext contextdb)
        {
            _context = contextdb;
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

                var query = _context.EpeExemptionrequests.Where(u => u.Isdelete == 0 && u.Status >= 1);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchWords = search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // Start with TRUE so we can AND each word
                    var predicate = PredicateBuilder.New<EpeExemptionrequest>(true);
                    foreach (var word in searchWords)
                    {
                        predicate = predicate.And(u =>
                            u.Id.ToString().Contains(word) ||
                            u.Transactionnumber.ToLower().Contains(word) ||
                            u.Status.ToString().Contains(word) ||
                            u.Policyid.ToString().Contains(word)
                        );
                    }

                    query = query.Where(predicate);
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(u => u.Createddate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    items = items,
                    pagination = new
                    {
                        totalCount,
                        currentPage = page,
                        pageSize,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    },
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }

        }

        [HttpPost("{id}")]
        public async Task<IActionResult> GetExcemptionListById([FromQuery] int id)
        {
            try
            {
                var item = await _context.EpeExemptionrequests
                    .Where(u => u.Id == id && u.Isdelete == 0)
                    .FirstOrDefaultAsync();
                if (item == null)
                {
                    return NotFound(new { success = false, message = "Exemption request not found" });
                }
                return Ok(new { success = true, item });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }

        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateExemptionRequest([FromBody] EpeExemptionrequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                request.Createdbyuserid = request.Requesterid;
                request.Transactionnumber = "EPE" + DateTime.UtcNow.Ticks;
                request.Createddate = DateTime.UtcNow;
                _context.EpeExemptionrequests.Add(request);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Exemption request created successfully", item = request });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error" + e.Message);
            }

        }
    }
}
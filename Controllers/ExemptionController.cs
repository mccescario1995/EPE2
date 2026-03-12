using EPEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinqKit;

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExceptionController : ControllerBase
    {
        private readonly EpeDbContext _context;
        public ExceptionController(EpeDbContext contextdb)
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

        [HttpPost("{id}")]
        public async Task<IActionResult> GetExcemptionListById([FromQuery] int id)
        {
            try
            {
                var item = await _context.EpeTransactionheaders
                    .Where(u => u.Transactionid == id && u.Isdelete == 0)
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
        public async Task<IActionResult> CreateExemptionRequest([FromBody] EpeTransactionheader request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                request.Createdby = userId.HasValue ? userId.Value : 0;
                request.Createddate = DateTime.UtcNow;
                _context.EpeTransactionheaders.Add(request);
                
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
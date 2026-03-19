using EPEAPI.Models;
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

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> EditExcemptionListId([FromQuery] int id)
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

                var userDetails = _context.CoreVEmployeeDetails.Where( e => e.EmplId == requesterId).FirstOrDefaultAsync();
                
                request.Corporate = 
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

                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Exemption request created successfully", item = request });
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error" + e.Message);
            }

        }

        [HttpPost("{id}")]
        public async Task<IActionResult> UpdateExemptionRequest(int id, [FromBody] EpeTransactionheader request)
        {
            try
            {
                var existingItem = await _context.EpeTransactionheaders
                    .Where(u => u.Transactionid == id && u.Isdelete== 0)
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

                return Ok(new { success = true, message = "Exemption request updated successfully", item = existingItem });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error" + e.Message);
            }
        }
    }
}
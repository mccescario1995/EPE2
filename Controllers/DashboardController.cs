using EPEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly EpeDbContext _context;

        public DashboardController(EpeDbContext context)
        {
            _context = context;
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics(
            [FromQuery] int year = 0,
            [FromQuery] int months = 6)
        {
            try
            {
                if (year == 0) year = DateTime.Now.Year;
                var fromDate = DateTime.Now.AddMonths(-months);

                // 1. Approval rates over time (monthly)
                var transactions = await _context.EpeTransactionheaders
                    .Where(t => t.Isdelete == 0 && t.Createddate >= fromDate)
                    .ToListAsync();

                var approvalRates = transactions
                    .GroupBy(t => new { t.Createddate.Year, t.Createddate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new
                    {
                        month = new DateTime(g.Key.Year, g.Key.Month, 1)
                            .ToString("MMM yyyy"),
                        total = g.Count(),
                        approved = g.Count(t => t.Finaldecision == "Approved"),
                        disapproved = g.Count(t => t.Finaldecision == "Disapproved"),
                        pending = g.Count(t => t.Finaldecision == "Pending"),
                        approvalRate = g.Count() == 0 ? 0 :
                            Math.Round((double)g.Count(t => t.Finaldecision == "Approved")
                            / g.Count() * 100, 1)
                    })
                    .ToList();

                // 2. Common policy overrides
                var policyOverrides = await _context.EpeVTransactionHeaders
                    .Where(t => t.IsDelete == 0)
                    .GroupBy(t => new { t.PolicyId, t.PolicyName })
                    .Select(g => new
                    {
                        policyId = g.Key.PolicyId,
                        policyName = g.Key.PolicyName ?? $"Policy {g.Key.PolicyId}",
                        totalRequests = g.Count(),
                        approved = g.Count(t => t.FinalDecision == "Approved"),
                        disapproved = g.Count(t => t.FinalDecision == "Disapproved"),
                        pending = g.Count(t => t.FinalDecision == "Pending"),
                    })
                    .OrderByDescending(p => p.totalRequests)
                    .Take(10)
                    .ToListAsync();

                // 3. Average processing times (approved transactions only)
                var approvedTrans = await _context.EpeTransactionheaders
                    .Where(t => t.Isdelete == 0
                        && t.Finaldecision == "Approved"
                        && t.Modifieddate != null
                        && t.Createddate >= fromDate)
                    .Select(t => new
                    {
                        policyId = t.Policyid,
                        created = t.Createddate,
                        modified = t.Modifieddate
                    })
                    .ToListAsync();

                var processingByPolicy = approvedTrans
                    .GroupBy(t => t.policyId)
                    .Select(g =>
                    {
                        var avgHours = g
                            .Where(t => t.modified.HasValue)
                            .Select(t => (t.modified!.Value - t.created).TotalHours)
                            .DefaultIfEmpty(0)
                            .Average();
                        return new
                        {
                            policyId = g.Key,
                            avgProcessingHours = Math.Round(avgHours, 1),
                            avgProcessingDays = Math.Round(avgHours / 24, 1),
                            count = g.Count()
                        };
                    })
                    .OrderByDescending(p => p.count)
                    .ToList();

                // Overall avg processing time
                var allApprovedHours = approvedTrans
                    .Where(t => t.modified.HasValue)
                    .Select(t => (t.modified!.Value - t.created).TotalHours)
                    .DefaultIfEmpty(0)
                    .ToList();

                var overallAvgHours = allApprovedHours.Any()
                    ? Math.Round(allApprovedHours.Average(), 1) : 0;

                // 4. Approver workload metrics
                var approverWorkload = await _context.EpeTransactionapprovers
                    .Where(a => a.Isdelete == 0)
                    .GroupBy(a => a.Approverid)
                    .Select(g => new
                    {
                        approverId = g.Key,
                        totalAssigned = g.Count(),
                        approved = g.Count(a => a.Respond == 1),
                        disapproved = g.Count(a => a.Respond == 2),
                        pending = g.Count(a => a.Respond == 0),
                    })
                    .OrderByDescending(a => a.totalAssigned)
                    .Take(10)
                    .ToListAsync();

                // Get approver names from policy approvers table
                var approverIds = approverWorkload
                    .Where(a => a.approverId != null)
                    .Select(a => int.Parse(a.approverId!))
                    .ToList();

                var approverDetails = await _context.EpePolicyapprovers
                    .Where(p => approverIds.Contains(p.Id))
                    .Select(p => new { id = p.Id.ToString(), name = p.Approvername })
                    .ToListAsync();

                var workloadWithNames = approverWorkload.Select(a => new
                {
                    a.approverId,
                    approverName = approverDetails
                        .FirstOrDefault(d => d.id == a.approverId)?.name
                        ?? $"Approver {a.approverId}",
                    a.totalAssigned,
                    a.approved,
                    a.disapproved,
                    a.pending,
                    responseRate = a.totalAssigned == 0 ? 0 :
                        Math.Round((double)(a.approved + a.disapproved) / a.totalAssigned * 100, 1)
                }).ToList();

                // 5. Summary KPIs
                var allTrans = await _context.EpeTransactionheaders
                    .Where(t => t.Isdelete == 0)
                    .ToListAsync();

                var summary = new
                {
                    totalRequests = allTrans.Count,
                    totalApproved = allTrans.Count(t => t.Finaldecision == "Approved"),
                    totalDisapproved = allTrans.Count(t => t.Finaldecision == "Disapproved"),
                    totalPending = allTrans.Count(t => t.Finaldecision == "Pending"),
                    overallApprovalRate = allTrans.Count == 0 ? 0 :
                        Math.Round((double)allTrans.Count(t => t.Finaldecision == "Approved")
                        / allTrans.Count * 100, 1),
                    avgProcessingHours = overallAvgHours,
                    avgProcessingDays = Math.Round(overallAvgHours / 24, 1),
                };

                return Ok(new
                {
                    success = true,
                    summary,
                    approvalRates,
                    policyOverrides,
                    processingByPolicy,
                    approverWorkload = workloadWithNames,
                    generatedAt = DateTime.Now
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, "Error: " + e.Message);
            }
        }
    }
}
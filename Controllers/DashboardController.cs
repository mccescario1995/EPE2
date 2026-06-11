using EPEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
            [FromQuery] int months = 6,
            [FromQuery] string period = "daily",
            [FromQuery] int count = 12)
        {
            try
            {
                if (year != 0)
                {
                    var fromYear = DateTime.Now.AddYears(1 - (year / 1000)); // Handle year input
                    months = (DateTime.Now.Year - fromYear.Year) * 12 + (DateTime.Now.Month - fromYear.Month);
                }

                DateTime fromDate;
                string periodLabel;

                switch (period.ToLower())
                {
                    case "daily":
                        fromDate = DateTime.Now.AddDays(-count);
                        periodLabel = "day";
                        break;
                    case "weekly":
                        fromDate = DateTime.Now.AddDays(-7 * count);
                        periodLabel = "week";
                        break;
                    case "monthly":
                    default:
                        fromDate = DateTime.Now.AddMonths(-count);
                        periodLabel = "month";
                        break;
                }

                // 1. Approval rates over time
                var transactions = await _context.EpeTransactionheaders
                    .Where(t => t.Isdelete == 0 && t.Createddate >= fromDate)
                    .ToListAsync();

                //var approvalRates = new List<object>();
                List<ApprovalRateDto> approvalRates = new();

                switch (period.ToLower())
                {
                    case "daily":
                        approvalRates = transactions.GroupBy(t => t.Createddate.Date)
                            .Select(g => new ApprovalRateDto
                            {
                                Period = g.Key.ToString("MMM dd, yyyy"),
                                Total = g.Count(),
                                Approved = g.Count(t => t.Finaldecision == "Approved"),
                                Disapproved = g.Count(t => t.Finaldecision == "Disapproved"),
                                Pending = g.Count(t => t.Finaldecision == "Pending"),
                                ApprovalRate = g.Count() == 0 ? 0 :
                                    Math.Round((double)g.Count(t => t.Finaldecision == "Approved")
                                    / g.Count() * 100, 1)
                            })
                            .ToList();
                        break;

                    case "weekly":
                        var weekGroups = transactions
                            .GroupBy(t => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                t.Createddate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday))
                            .Select(g => new ApprovalRateDto
                            {
                                Period = $"Week {g.Key}",
                                Total = g.Count(),
                                Approved = g.Count(t => t.Finaldecision == "Approved"),
                                Disapproved = g.Count(t => t.Finaldecision == "Disapproved"),
                                Pending = g.Count(t => t.Finaldecision == "Pending"),
                                ApprovalRate = g.Count() == 0 ? 0 :
                                    Math.Round((double)g.Count(t => t.Finaldecision == "Approved")
                                    / g.Count() * 100, 1)
                            })
                            .ToList();
                        approvalRates = weekGroups;
                        break;

                    default:
                        approvalRates = transactions
                            .GroupBy(t => new { t.Createddate.Year, t.Createddate.Month })
                            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                            .Select(g => new ApprovalRateDto
                            {
                                Period = new DateTime(g.Key.Year, g.Key.Month, 1)
                                    .ToString("MMM yyyy"),
                                Total = g.Count(),
                                Approved = g.Count(t => t.Finaldecision == "Approved"),
                                Disapproved = g.Count(t => t.Finaldecision == "Disapproved"),
                                Pending = g.Count(t => t.Finaldecision == "Pending"),
                                ApprovalRate = g.Count() == 0 ? 0 :
                                    Math.Round((double)g.Count(t => t.Finaldecision == "Approved")
                                    / g.Count() * 100, 1)
                            })
                            .ToList();
                        break;
                }

                // 2. Common policy overrides (unchanged - all time data)
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

                // 3. Average processing times (approved transactions only, filtered by period)
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

        public class ApprovalRateDto
        {
            public string Period { get; set; } = "";
            public int Total { get; set; }
            public int Approved { get; set; }
            public int Disapproved { get; set; }
            public int Pending { get; set; }
            public double ApprovalRate { get; set; }
        }
    }
}
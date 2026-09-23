using EPEAPI.Dto;
using EPEAPI.Models;
using EPEAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EPEAPI.Services;

public class ReminderEscalationService : BackgroundService
{
    private readonly ILogger<ReminderEscalationService> _logger;
    private readonly IOptions<ReminderEscalationSettings> _settings;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ReminderEscalationService(
        ILogger<ReminderEscalationService> logger,
        IOptions<ReminderEscalationSettings> settings,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _settings = settings;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reminder & Escalation Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAndEscalationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Reminder & Escalation Service cycle");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.Value.CheckIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Reminder & Escalation Service stopped");
    }

    private async Task ProcessRemindersAndEscalationsAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EpeDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<EmailNotificationService>();

        var now = DateTime.UtcNow.AddHours(8);

        var openTransactionIds = await context.EpeTransactionheaders
            .Where(h => h.Status == "O" && h.Isdelete == 0)
            .Select(h => h.Transactionid)
            .ToListAsync(ct);

        var openTransactionIdSet = openTransactionIds.ToHashSet();

        var stalledApprovers = await context.EpeTransactionapprovers
            .Where(a => a.Respond == 0
                && a.Isdelete == 0
                && a.Sentdate != null
                && a.Issent == 1
                && openTransactionIds.Contains(a.Transactionid))
            .Take(50)
            .ToListAsync(ct);

        foreach (var approver in stalledApprovers)
        {
            try
            {
                await ProcessApproverAsync(context, emailService, approver, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approver {ApproverId} for transaction {TransactionId}",
                    approver.Approverid, approver.Transactionid);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private async Task ProcessApproverAsync(
        EpeDbContext context,
        EmailNotificationService emailService,
        EpeTransactionapprover approver,
        DateTime now)
    {
        var header = await context.EpeVTransactionHeaders
            .Where(h => h.TransactionId == approver.Transactionid && h.IsDelete == 0)
            .FirstOrDefaultAsync();

        if (header == null) return;

        var requester = await context.CoreVEmployeeDetails
            .Where(e => e.EmplId == header.RequesterId)
            .FirstOrDefaultAsync();

        var approverEmail = "";
        var approverName = "";

        if (approver.Approvertype == "IM" && requester != null)
        {
            approverEmail = requester.ImmediateEmail ?? "";
            approverName = requester.ImmediateName ?? "";
        }
        else
        {
            var policyApprover = await context.EpePolicyapprovers
                .Where(e => e.Id == int.Parse(approver.Approverid))
                .FirstOrDefaultAsync();
            approverEmail = policyApprover?.Approveremail ?? "";
            approverName = policyApprover?.Approvername ?? "";
        }

        if (string.IsNullOrEmpty(approverEmail)) return;

        int approverUserId = 0;
        if (!string.IsNullOrEmpty(approver.Approverid))
        {
            var approverUser = await context.CoreVUsers
                .Where(u => u.EmplId == approver.Approverid)
                .FirstOrDefaultAsync();
            if (approverUser != null)
                approverUserId = approverUser.UserId;
        }

        var referenceTime = approver.Lastsentdate ?? approver.Sentdate;
        if (referenceTime == null) return;

        var hoursSinceLastSend = (now - referenceTime.Value).TotalHours;

        if (hoursSinceLastSend >= _settings.Value.DefaultEscalationIntervalHours)
        {
            await EscalateAsync(context, emailService, approver, header, requester, approverEmail, approverName, approverUserId, hoursSinceLastSend);
        }
        else if (hoursSinceLastSend >= _settings.Value.DefaultReminderIntervalHours
            && approver.Sentcount < _settings.Value.DefaultMaxReminders)
        {
            await SendReminderAsync(context, emailService, approver, header, requester, approverEmail, approverName, approverUserId);
        }
    }

    private async Task SendReminderAsync(
        EpeDbContext context,
        EmailNotificationService emailService,
        EpeTransactionapprover approver,
        EpeVTransactionHeader header,
        CoreVEmployeeDetail? requester,
        string approverEmail,
        string approverName,
        int approverUserId)
    {
        var transApproverRecord = await context.EpeTransactionapprovers
            .Where(a => a.Transactionid == header.TransactionId
                && a.Approverid == approver.Approverid
                && a.Isdelete == 0)
            .FirstOrDefaultAsync();

        if (approver.Linesequence == 1)
        {
            await emailService.SendMainApproverReminderEmailAsync(
                approverEmail, approverName, header.TransactionId,
                header.TransactionNumber, requester?.Employeename2 ?? header.RequesterId,
                header.Details ?? "No details provided",
                approverUserId, transApproverRecord?.Transapproverid ?? 0);
        }
        else
        {
            var mainApproverRecord = await context.EpeTransactionapprovers
                .Where(a => a.Transactionid == approver.Transactionid
                    && a.Sort == approver.Sort
                    && a.Linesequence == 1
                    && a.Isdelete == 0)
                .FirstOrDefaultAsync();

            string mainApproverName = "Main Approver";
            if (mainApproverRecord != null)
            {
                if (mainApproverRecord.Approvertype == "IM" && requester != null)
                    mainApproverName = requester.ImmediateName ?? "Immediate Head";
                else
                {
                    var policyMainApprover = await context.EpePolicyapprovers
                        .Where(e => e.Id == int.Parse(mainApproverRecord.Approverid))
                        .FirstOrDefaultAsync();
                    mainApproverName = policyMainApprover?.Approvername ?? "Main Approver";
                }
            }

            await emailService.SendProxyApproverReminderEmailAsync(
                approverEmail, approverName, mainApproverName,
                header.TransactionId, header.TransactionNumber,
                requester?.Employeename2 ?? header.RequesterId,
                header.Details ?? "No details provided",
                approverUserId, transApproverRecord?.Transapproverid ?? 0);
        }

        approver.Issent = 1;
        approver.Sentcount += 1;
        approver.Lastsentdate = DateTime.UtcNow.AddHours(8);
        if (approver.Sentdate == null)
            approver.Sentdate = DateTime.UtcNow.AddHours(8);
    }

    private async Task EscalateAsync(
        EpeDbContext context,
        EmailNotificationService emailService,
        EpeTransactionapprover approver,
        EpeVTransactionHeader header,
        CoreVEmployeeDetail? requester,
        string approverEmail,
        string approverName,
        int approverUserId,
        double hoursSinceLastSend)
    {
        var escalationEmail = _settings.Value.EscalationNotifyEmail;
        if (string.IsNullOrWhiteSpace(escalationEmail)) return;

        await emailService.SendEscalationEmailAsync(
            escalationEmail,
            header.TransactionNumber,
            requester?.Employeename2 ?? header.RequesterId,
            header.Details ?? "No details provided",
            approverName,
            approverEmail,
            (int)hoursSinceLastSend);

        if (approverUserId > 0)
        {
            await emailService.CreateWebNotificationAsync(
                approverUserId,
                "Approval Escalation",
                $"Escalation: Exemption request ({header.TransactionNumber}) has been pending for {hoursSinceLastSend:0} hours",
                "error",
                $"/main/pending-for-approval/approve?id={header.TransactionId}");
        }

        var transactionLog = new EpeTransactionlog
        {
            TransactionId = header.TransactionId,
            PolicyId = header.PolicyId,
            TransactionType = "ESCALATE",
            PreviousStatus = header.Status,
            NewStatus = header.Status,
            PerformedBy = 0,
            PerformedAt = DateTime.UtcNow.AddHours(8),
            Remarks = $"Automated escalation - approver {approverName} ({approverEmail}) pending for {hoursSinceLastSend:0} hours",
            Reason = "Approval reminder escalation"
        };
        context.EpeTransactionlogs.Add(transactionLog);

        approver.Sentcount = _settings.Value.DefaultMaxReminders + 1;
    }

    public async Task ProcessOnceAsync(CancellationToken ct = default)
    {
        await ProcessRemindersAndEscalationsAsync(ct);
    }
}
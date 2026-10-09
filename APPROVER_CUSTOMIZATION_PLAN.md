# Approver Flow Customization Plan

## Requirements Summary

1. **Requestor Removal**: If requestor is an approver (main or proxy), remove them entirely from `EpeTransactionapprovers`. If main approver removed → remove entire Sort group (main + proxies). If proxy removed → remove only that proxy.

2. **Main Approver Disapproval**: If main approver (Linesequence=1) disapproves, hide rest of group (proxies) from UI/list. Only the disapproving approver gets `Respond=2`; no auto-disapproval message for others.

3. **IM as Last Approver**: If Immediate Head (Approvertype="IM") is in the last Sort group, skip all subsequent approvers (don't transfer to `EpeTransactionapprovers`).

---

## Current Code Locations to Modify

### Primary: `Controllers/ExemptionController.cs`
- **CreateExemptionRequest** (lines ~1080-1385): Initial transaction creation with approvers
- **SubmitDraft** (lines ~1590-1760): Draft submission with approvers
- **RespondToApproval** (lines ~2067-2433): Approval/disapproval handling

### Secondary: `EmailApprovalController.cs` (if email approval uses same logic)

---

## Detailed Implementation Plan

### Phase 1: Requestor Removal Logic (New Helper Method)

**Location**: Add private method in `ExemptionController.cs`

```csharp
private List<EpePolicyapprover> FilterRequesterApprovers(
    List<EpePolicyapprover> policyApprovers,
    string requesterId)
{
    // 1. Find all Sort groups where requester exists
    var requesterSortGroups = policyApprovers
        .Where(p => p.Approverid == requesterId)
        .Select(p => p.Sort)
        .Distinct()
        .ToHashSet();

    // 2. For each Sort group requester belongs to:
    //    - If requester is main (Linesequence=1) → remove ENTIRE group (all Linesequence)
    //    - If requester is proxy (Linesequence>1) → remove ONLY that proxy
    
    var filtered = policyApprovers
        .Where(p => {
            if (!requesterSortGroups.Contains(p.Sort)) return true; // Keep other groups
            
            // In requester's group(s)
            if (p.Approverid == requesterId) return false; // Remove requester
            
            // If requester was main in this group, remove all proxies too
            var requesterInGroup = policyApprovers
                .FirstOrDefault(r => r.Sort == p.Sort && r.Approverid == requesterId);
            if (requesterInGroup?.Linesequence == 1) return false; // Remove entire group
            
            return true; // Keep other proxies if requester was just a proxy
        })
        .ToList();

    return filtered;
}
```

**Apply in two places**:
- `CreateExemptionRequest` (after line 1093, before line 1104)
- `SubmitDraft` (after line 1601, before line 1610)

**Also update**: `requesterSortGroups` calculation should use `filteredApprovers` instead of original `policyApprovers`

---

### Phase 2: IM as Last Approver Logic (New Helper Method)

**Location**: Add private method in `ExemptionController.cs`

```csharp
private List<EpePolicyapprover> FilterIMLastApprover(
    List<EpePolicyapprover> policyApprovers)
{
    // Find the maximum Sort value (last group)
    var maxSort = policyApprovers.Max(p => p.Sort ?? 0);
    
    // Check if last group has any IM approver
    var lastGroupHasIM = policyApprovers
        .Any(p => p.Sort == maxSort && p.Approvertype == "IM");
    
    if (!lastGroupHasIM) return policyApprovers;
    
    // Remove all approvers in Sort groups AFTER the last group (should be none)
    // But also: if IM is in last group, that's the final approver
    // So we keep only up to and including the IM's Sort group
    
    // Actually, requirement says: "if IM is the last approver, rest should be gone"
    // This means: if IM exists in the highest Sort, truncate at that Sort
    var imSort = policyApprovers
        .Where(p => p.Approvertype == "IM")
        .Max(p => p.Sort ?? 0);
    
    if (imSort > 0)
    {
        return policyApprovers.Where(p => (p.Sort ?? 0) <= imSort).ToList();
    }
    
    return policyApprovers;
}
```

**Apply in two places** (after requestor filtering):
- `CreateExemptionRequest` 
- `SubmitDraft`

---

### Phase 3: Disapproval Logic Changes

**Location**: `RespondToApproval` method (lines ~2341-2422)

**Current behavior** (lines 2343-2375):
- Main approver disapproves → auto-disapproves ALL in same Sort group
- Sets auto-disapproval message: `"Disapproved by: {empName}"`

**New behavior**:
```csharp
else if (request.Respond == 2) // Disapproved
{
    // Check if current approver is MAIN (Linesequence == 1)
    var isMainApprover = approver.Linesequence == 1;
    
    if (isMainApprover)
    {
        // Main approver disapproved - hide proxies from UI
        // Option A: Mark proxies as Isdelete = 1 (soft delete)
        // Option B: Leave them but frontend filters by Linesequence when main has Respond=2
        
        // For now: Soft delete proxies in same Sort group
        var proxyApprovers = await _context.EpeTransactionapprovers
            .Where(a => a.Transactionid == request.TransactionId
                     && a.Sort == approver.Sort
                     && a.Linesequence != 1
                     && a.Isdelete == 0)
            .ToListAsync();
        
        foreach (var proxy in proxyApprovers)
        {
            proxy.Isdelete = 1; // Hide from UI
        }
    }
    
    // Only the acting approver gets Respond=2 with their remarks
    // NO auto-disapproval for others
    approver.Respond = 2;
    approver.Responddate = DateTime.UtcNow.AddHours(8);
    approver.Remarks = request.Remarks; // User's actual remarks
    
    await _context.SaveChangesAsync();
    
    // Close transaction
    await _context.EpeTransactionheaders
        .Where(h => h.Transactionid == request.TransactionId)
        .ExecuteUpdateAsync(b => b
            .SetProperty(h => h.Status, "C")
            .SetProperty(h => h.Finaldecision, "Disapproved"));
    
    // ... logging and email (unchanged)
}
```

---

### Phase 4: Frontend Considerations (Not in this backend plan)

**UI needs to handle**:
1. When main approver has `Respond=2`, hide proxies (Linesequence > 1) in same Sort
2. When requester was removed, they won't appear in approver list at all
3. When IM is last approver, no further groups exist

---

## Testing Scenarios

| Scenario | Expected Result |
|----------|-----------------|
| Requester = Main approver (Linesequence=1) in Sort 1 | Entire Sort 1 removed; Sort 2 becomes first |
| Requester = Proxy (Linesequence=2) in Sort 1 | Only that proxy removed; main (Linesequence=1) stays |
| Requester in multiple Sort groups | Apply removal per group independently |
| Main approver disapproves | Transaction closes; proxies hidden (Isdelete=1); only main has Respond=2 |
| Proxy disapproves | Only that proxy gets Respond=2; main stays pending; transaction closes |
| IM in highest Sort | All groups after IM's Sort removed; IM's group is final |
| IM not in highest Sort | No truncation; normal flow |

---

## Files to Modify

1. **`Controllers/ExemptionController.cs`** - Main changes
   - Add `FilterRequesterApprovers()` helper
   - Add `FilterIMLastApprover()` helper
   - Update `CreateExemptionRequest` (2 filter calls)
   - Update `SubmitDraft` (2 filter calls)
   - Update `RespondToApproval` disapproval logic

2. **Potential**: `EmailApprovalController.cs` - Verify `ProcessApproval`/`HandleDisapproval` don't need similar changes (they reuse logic but may need sync)

---

## Implementation Order

1. Add helper methods
2. Apply requestor filter in `CreateExemptionRequest`
3. Apply requestor filter in `SubmitDraft`
4. Apply IM-last filter in both
5. Update disapproval logic in `RespondToApproval`
6. Test all scenarios

---

## Notes

- **Do NOT modify**: `useApiFetch.ts`, `useAuth.ts` (per project constraints)
- **Encryption**: Use `app/utils/storageCrypto.ts` not `encrypt.js` (per project constraints)
- **Email tokens**: `EmailApprovalController` uses tokens; ensure disapproval flow still generates correct redirect
- **Reminder service**: `ReminderEscalationService` reads `EpeTransactionapprovers`; proxies marked `Isdelete=1` will be skipped automatically
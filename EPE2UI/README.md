# EPE — Executive Process Exemption System

A full-stack exemption request management system built with **Nuxt 3 / Vue.js** on the frontend and **ASP.NET Core / C#** on the backend.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Environment Variables](#environment-variables)
- [Features](#features)
- [Approval Workflow](#approval-workflow)
- [Email Approval Flow](#email-approval-flow)
- [Key Components](#key-components)
- [API Reference](#api-reference)
- [Development Notes](#development-notes)

---

## Overview

EPE allows employees to submit exemption requests for policy overrides. Requests go through a sequential multi-approver workflow where each step must be satisfied before the next is notified. Approvers can respond via the web app or directly from email links.

---

## Tech Stack

### Frontend
| Technology | Purpose |
|---|---|
| Nuxt 3 (SSR disabled, hash-mode routing) | SPA framework |
| Vue 3 + TypeScript | Component logic |
| Tailwind CSS | Styling |
| Pinia | State management |
| vue-draggable-plus | Drag-and-drop approver ordering |
| date-fns | Date formatting |
| SheetJS (xlsx) | Excel export |

### Backend
| Technology | Purpose |
|---|---|
| ASP.NET Core 8 / C# | REST API |
| Entity Framework Core 8 | ORM |
| SQL Server | Database |
| LinqKit | Dynamic query predicates |
| Custom TokenManager | Session authentication |

---

## Project Structure

```
/
├── app/
│   ├── components/
│   │   ├── BaseTable.vue          # Reusable table with server pagination, sort, filter, export
│   │   ├── Dropdown.vue           # Single/multi-select with async fetch support
│   │   ├── Modal.vue              # Stacked modal system with nested modal support
│   │   ├── ManageApprovers.vue    # Drag-and-drop approver management (in-modal)
│   │   ├── ManageApprover.vue     # Alternative approver manager variant
│   │   ├── PolicyApprover.vue     # Standalone full-page approver manager
│   │   └── ...icons, charts, etc
│   ├── composables/
│   │   ├── useApiFetch.ts         # Unified fetch wrapper with token refresh and retry
│   │   ├── useAuth.ts             # Login / logout / token refresh
│   │   ├── useToast.ts            # Global toast notification store
│   │   ├── useLoading.ts          # Global full-screen loading overlay
│   │   ├── useConfirmModal.ts     # Promise-based confirmation dialog
│   │   └── useBreadcrumbs.ts      # Auto-generated breadcrumbs from route path
│   ├── pages/
│   │   ├── dashboard/             # Analytics dashboard
│   │   ├── exemptions/
│   │   │   ├── all/               # Server-paginated list of all requests
│   │   │   ├── pending/           # Pending requests list
│   │   │   ├── closed/            # Closed requests list
│   │   │   ├── create.vue         # New exemption form
│   │   │   ├── edit.vue           # Edit existing request + resend approver emails
│   │   │   ├── view.vue           # Read-only request detail
│   │   │   └── approve.vue        # Approver response form
│   │   ├── maintenance/policies/  # Policy and approver maintenance
│   │   └── email-response.vue     # Email link approval page (hash-mode safe)
│   └── layouts/
│       ├── default.vue            # Main layout with sidebar + topbar
│       └── empty.vue              # Bare layout for email-response
│
├── Controllers/
│   ├── AccessController.cs        # Login endpoint
│   ├── ExemptionController.cs     # Full exemption CRUD + approval workflow
│   ├── MaintenanceController.cs   # Policy and approver CRUD
│   ├── EmailApprovalController.cs # Email token preview and action endpoints
│   ├── DashboardController.cs     # Analytics aggregation
│   └── NotificationController.cs  # In-app notification CRUD
├── Services/
│   ├── EmailNotificationService.cs # All email templates and sending logic
│   ├── EmailApprovalTokenService.cs# One-time email token generation
│   └── FileUploadService.cs        # Attachment validation and storage
├── Models/                         # EF entity models and DB views
├── Dto/                            # Request/response DTOs
└── TokenAuthentication/            # Custom base64 token auth (TokenManager)
```

---

## Getting Started

### Prerequisites

- Node.js 18+
- .NET 8 SDK
- SQL Server (connection string in `appsettings.json`)

### Frontend

```bash
# Install dependencies
npm install

# Development server (http://localhost:3000)
npm run dev

# Production build
npm run build
npm run generate   # static output
```

### Backend

```bash
# Restore packages
dotnet restore

# Run (http://localhost:5000)
dotnet run

# Apply EF migrations (if needed)
dotnet ef database update
```

---

## Environment Variables

### Frontend (`nuxt.config.ts` / `.env`)

```env
NUXT_PUBLIC_API=https://your-api-host/epe2apidev
```

### Backend (`appsettings.Production.json`)

```json
{
  "ConnectionStrings": {
    "sql-cons": "data source=...;initial catalog=EPEDB_DEV;..."
  },
  "UrlBase": {
    "Api": "https://your-api-host/epe2apidev",
    "View": "https://your-frontend-host/epe2dev"
  }
}
```

---

## Features

### Exemption Requests
- **Create** — select policy, enter details, attach up to 5 files (10 MB each, 50 MB total)
- **Draft** — save a partial request without triggering emails
- **Edit** — update details, manage attachments, resend approver reminders
- **View** — full read-only detail with approver status timeline
- **Delete** — soft delete (sets `isdelete = 1`)
- **Submit Draft** — promotes a draft to a full pending request

### Approval Workflow
- Sequential multi-step approval with main + proxy approver grouping
- Approvers respond via the web app or one-click email links
- Auto-approves all other members of the same sort group when one responds
- Sends next-step email notifications automatically

### Policy Maintenance
- Create / edit / delete policies
- Drag-and-drop approver sequence manager with proxy support
- IM (Immediate Head) type resolved dynamically at submission time

### Dashboard
- Approval rate trends (daily / weekly / monthly)
- Policy override frequency table
- Overall decision breakdown bar

### Notifications
- In-app notification bell
- Email: submission confirmation, approver request, status change, proxy reminder

---

## Approval Workflow

The approval model uses `Sort` and `Linesequence` to define structure:

```
Sort 1  (Step 1)
  ├── Linesequence 1  → Main Approver A   (must respond, OR any proxy in group)
  └── Linesequence 2  → Proxy Approver B  (can respond instead of A)

Sort 2  (Step 2)
  └── Linesequence 1  → Main Approver C   (notified only after Sort 1 is satisfied)
```

**Rules:**
- Approvers sharing the same `Sort` value form one group. Any one of them responding satisfies the whole group.
- When a group is satisfied, all remaining unanswered members in the group are auto-marked.
- Sequential progression: Sort 2 is only notified after Sort 1 is satisfied.
- Any disapproval immediately closes the request (`finalDecision = Disapproved`).

---

## Email Approval Flow

The two-step architecture separates token validation from action execution:

```
Email button click
       │
       ▼
GET /api/emailapproval/preview?token=...
       │
       ├─ Browser (non-XHR) → Redirect to /#/email-response?token=...&action=...
       └─ XHR / Accept:application/json → Return JSON preview
                                                │
                                        User confirms on email-response.vue
                                                │
                                                ▼
                              GET /api/emailapproval/act?token=...
                                                │
                                  Process approval, mark token used
```

**Token lifecycle:**
- Generated at request creation (one approve + one disapprove token per approver)
- 72-hour expiry
- Single-use (`isused = 1` after first action)
- Checks: token validity → not used → not expired → transaction not closed → approver not already responded

---

## Key Components

### `BaseTable.vue`
Reusable table with full server-side pagination support.

```vue
<base-table
  :server-pagination="true"
  :fetch-data="fetchFn"          <!-- async (params) => { data, total } -->
  :pagination="true"
  :per-page="10"
  :sortable-columns="['name', 'date']"
  :filterable-columns="['status']"
  :badge-fields="['status']"
  :field-mappings="{ status: { O: 'Open', C: 'Closed' } }"
  :field-colors="{ status: { O: 'bg-blue-100 text-blue-800' } }"
  :actions="[{ label: IconView, handler: viewFn, tooltip: 'View' }]"
/>
```

**Server pagination contract:**
```ts
fetchFn: (params: {
  page: number;
  limit: number;
  search: string;
  filters: Record<string, string>;
  sortField?: string;
  sortDirection?: 'asc' | 'desc';
}) => Promise<{ data: any[]; total: number }>
```

### `apiFetch` (composable)
Unified fetch wrapper used everywhere in place of `$fetch` or native `fetch`.

```ts
// Auto-attaches token, handles 401 → refresh → retry (GET only)
const data = await apiFetch('/api/exemption', {
  method: 'POST',
  body: JSON.stringify(payload),
  parseJson: true,   // auto-parse JSON response
});
```

> **Important:** Use `apiFetch` (direct, bypasses Nuxt SSR proxy) instead of `$fetch` for all .NET API calls.

### `ManageApprovers.vue`
Drop-in modal component for managing policy approvers with drag-and-drop reordering.

```vue
<ManageApprovers
  :show="approversModalOpen"
  :policy="{ policyId: 1, policyid: 1, description: 'Policy Name' }"
  @close="approversModalOpen = false"
  @saved="fetchPolicies"
/>
```

### `useToast` / `useLoading` / `useConfirmModal`
Global singletons — importable from anywhere.

```ts
import { showToast, startLoading, stopLoading } from '~/stores/globalvar';
const { open } = useConfirmModal();

startLoading({ loadingText: 'Saving...', spinnerColor: 'blue' });
const confirmed = await open({ message: 'Delete?', type: 'delete' });
stopLoading();
```

---

## API Reference

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/Access` | Login |
| POST | `/api/exemption` | List exemptions (server-paginated) |
| POST | `/api/exemption/create` | Create exemption (multipart/form-data) |
| POST | `/api/exemption/save-draft` | Save draft |
| POST | `/api/exemption/view/{id}` | Get exemption + approvers |
| POST | `/api/exemption/edit/{id}` | Get exemption for editing |
| POST | `/api/exemption/update/{id}` | Update exemption |
| POST | `/api/exemption/update-draft/{id}` | Update draft fields |
| POST | `/api/exemption/submit-draft/{id}` | Promote draft → pending |
| POST | `/api/exemption/delete/{id}` | Soft delete |
| POST | `/api/exemption/respond` | Submit approval/disapproval |
| POST | `/api/exemption/resend-main/{id}` | Resend reminder to main approver |
| POST | `/api/exemption/resend-proxy/{id}` | Resend reminder to proxy approver |
| GET | `/api/exemption/attachments/{id}` | List attachments |
| POST | `/api/exemption/add-attachment/{id}` | Upload files |
| POST | `/api/exemption/delete-attachment/{id}` | Delete file |
| GET | `/api/Maintenance/policy` | List all policies |
| POST | `/api/Maintenance/policy/create` | Create policy |
| POST | `/api/Maintenance/policy/update/{id}` | Update policy |
| POST | `/api/Maintenance/policy/delete/{id}` | Soft delete policy |
| POST | `/api/Maintenance/policy/{id}/approvers` | Get policy approvers |
| POST | `/api/Maintenance/policy/{id}/approvers/create` | Add approver |
| POST | `/api/Maintenance/policy/{id}/approvers/reorder` | Reorder approvers |
| POST | `/api/Maintenance/policy/{id}/approvers/delete/{aid}` | Remove approver |
| GET | `/api/Maintenance/employees/search?q=` | Employee search |
| GET | `/api/emailapproval/preview?token=` | Validate email token |
| GET | `/api/emailapproval/act?token=` | Execute email approval action |
| GET | `/api/dashboard/analytics` | Dashboard aggregations |
| POST | `/api/notification` | List notifications |
| POST | `/api/notification/{id}/read` | Mark notification read |

---

## Development Notes

### Known Patterns

**Vue reactivity with nested objects** — Use spread to trigger reactivity:
```ts
// ✅ Correct
steps.value[idx] = { ...steps.value[idx], field: newVal };
// ❌ Wrong — won't trigger reactivity
steps.value[idx].field = newVal;
```

**C# FormData binding** — `[FromForm]` property names are case-sensitive and must match the FormData keys exactly:
```ts
formData.append('Policyid', id.toString());  // Must match C# property casing
```

**Email method distinction:**
- Main approvers → `SendMainApproverReminderEmailAsync`
- Proxy approvers → `SendProxyApproverReminderEmailAsync`
- Do **not** substitute `SendApprovalRequestEmailAsync` for reminders

**Approver grouping** — Always group by `Sort`, not `Linesequence`. Approvers sharing the same `Sort` form one group regardless of their `Linesequence` value.

**Hash-mode routing** — The app uses `hashMode: true`. Token extraction from email links must parse `window.location.hash` manually:
```ts
const hash = window.location.hash;        // e.g. #/email-response?token=...
const params = new URLSearchParams(hash.slice(hash.indexOf('?') + 1));
const token = params.get('token');
```

**`$fetch` vs `apiFetch`** — Nuxt's `$fetch` proxies through the server side, which breaks direct .NET API calls. Always use the `apiFetch` composable instead.

### File Upload Limits
- Max files per request: **5**
- Max file size per file: **10 MB**
- Max total upload size: **50 MB**
- Allowed types: `pdf, doc, docx, xls, xlsx, txt, jpg, jpeg, png`
- Storage path: `wwwroot/uploads/attachments/`

### Token Authentication
The system uses a custom base64 token format (not JWT) managed by `TokenManager`. Tokens are stored in the `tokens` DB view and validated on each request via `TokenAuthenticationFilter`. Token expiry is 24 hours.
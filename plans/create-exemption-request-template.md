# Create Exemption Request Template Plan

## Overview
Create a new Vue page at `/exemptions/create` for submitting exemption requests. This template will provide a form for employees to request policy exemptions through the EPE system.

---

## Data Model Reference: epe_transactionheader

| Field | Type | Required | User Input? | Description |
|-------|------|----------|-------------|-------------|
| `Transactionid` | long | Auto | No | Primary key, auto-generated |
| `Transactionnumber` | string | Auto | No | Auto-generated |
| `Requestorid` | string | Auto | No | From session/auth |
| `Corporate` | string | No | **Yes** | Corporate identifier |
| `Policyid` | int | **Yes** | **Yes** | Foreign key to policy (required) |
| `Details` | string | **Yes** | **Yes** | Reason/details for exemption request (required) |
| `Status` | string | Auto | No | Default: 'O' (Open) |
| `Remarks` | string | No | No | For approver comments |
| `Finaldecision` | string | Auto | No | Default: 'Pending' |
| `Isdelete` | byte | Auto | No | Default: 0 |
| `Createdby` | int | Auto | No | From session |
| `Createddate` | DateTime | Auto | No | Auto-set |
| `Modifiedby` | int | No | No | For updates |
| `Modifieddate` | DateTime | No | No | For updates |
| `Workstation` | string | Auto | No | Auto-captured |
| `Ipaddress` | string | Auto | No | Auto-captured |
| `Macaddress` | string | Auto | No | Auto-captured |

---

## Form Fields (User Input Only)

Based on the epe_transactionheader table, the create form should include:

| Field | Component | Validation | Maps To |
|-------|-----------|------------|---------|
| Policy | Dropdown | Required | `Policyid` |
| Corporate | BaseInput | Optional | `Corporate` |
| Details | BaseInput (textarea) | Required, min 10 chars | `Details` |

---

## API Endpoints

### Create Exemption (Existing)
- **Endpoint**: `POST /api/exemption/create`
- **Request Body** (matches epe_transactionheader):
```json
{
  "policyid": 1,
  "corporate": "CORP-001",
  "details": "Requesting exemption for..."
}
```
- **Auto-set by API**:
  - `Status` = 'O' (Open)
  - `Finaldecision` = 'Pending'
  - `Isdelete` = 0
  - `Createdby` = from session
  - `Createddate` = current datetime

### Required: Policy List Endpoint
- **Suggested**: `GET /api/policy`
- Returns list of policies for dropdown

---

## UI/UX Specification

### Page Layout
```
/exemptions/create
├── Header (back button + "Create Exemption Request")
├── Form Card
│   ├── Policy Dropdown (required) *
│   ├── Corporate Dropdown (optional)
│   ├── Details Textarea (required) *
│   └── Action Buttons
│       ├── Cancel → /exemptions
│       └── Submit → POST /api/exemption/create
└── Toast: Success/Error feedback
```

### Components Used
| Component | Purpose |
|-----------|---------|
| `Button` | Back button, Submit, Cancel |
| `Card` | Form container |
| `Dropdown` | Policy and Corporate selection |
| `BaseInput` | Details (textarea mode) |
| `Toast` | Success/error notifications |

### Form Fields (User Input Only)

| Field | Component | Validation | Maps To | Options |
|-------|-----------|------------|---------|---------|
| Policy | Dropdown | Required | `Policyid` | Fetch from /api/policy |
| Corporate | Dropdown | Optional | `Corporate` | FS, FL, FTM |
| Details | BaseInput (textarea) | Required, min 10 chars | `Details` | Reason for exemption |

### Visual Design
- Match existing exemptions page styling
- Use bg-gray-50 background
- White card with shadow for form container
- Same button styles as existing UI
- Responsive: max-w-7xl, centered

---

## Implementation Steps

### Step 1: Create the Create Exemption Page
- File: `EPEUI/app/pages/exemptions/create.vue`
- Implements form with validation
- Uses existing component library

### Step 2: Add Form State Management
- Reactive form data object
- Validation state tracking
- Loading state for submission

### Step 3: Implement Form Submission
- Call to `/api/exemption/create`
- Handle success/error responses
- Redirect to list on success

### Step 4: Add Navigation
- Back button returns to `/exemptions`
- Cancel button same as back

---

## File to Create
- `EPEUI/app/pages/exemptions/create.vue`

---

## Notes
- The user will handle the policy API endpoint
- Form validation should be implemented on client-side
- Status is automatically set to 'O' (Open) on creation
- Corporate field is optional per the data model

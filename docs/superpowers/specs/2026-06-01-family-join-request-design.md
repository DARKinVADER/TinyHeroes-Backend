# Family Join Request — Design Spec

**Date:** 2026-06-01
**Status:** Implemented

## Context

The existing invitation system is admin-initiated: an Admin generates a token or email invite and the recipient accepts it. This feature extends the flow in the opposite direction — a new user can proactively request to join a family using a short code. The Admin then approves or rejects the request inside the app.

## Data Model

### `Family.JoinCode` (new field)
- 8-char uppercase alphanumeric (e.g. `HERO4821`), unique index
- Generated on family creation, never changes unless regenerated
- Exposed in `FamilyDetailResponse` only for Admin members

### `FamilyJoinRequest` entity
```
Id              Guid
FamilyId        Guid  → FK Family (cascade)
RequestedById   Guid  → FK User (cascade)
Status          JoinRequestStatus (Pending | Approved | Rejected) stored as string
RequestedAt     DateTime
ResolvedAt      DateTime?
ResolvedById    Guid?  → FK User (restrict)
ResolvedBy      User? nav
```

## Backend API

### Requester-side (`JoinRequestController` at `api/join-requests`)
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/join-requests` | Submit a join request |
| GET | `/api/join-requests` | Get current user's pending request (404 if none) |
| DELETE | `/api/join-requests` | Cancel own pending request |

### Admin-side (on `FamilyController`)
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/families/join-requests` | List pending requests (admin only) |
| POST | `/api/families/join-requests/{id}/resolve` | Approve or reject |

### Backend security rules
- All endpoints validate against DB — localStorage flags are UX only
- POST /join-requests: user has no FamilyMember AND no existing Pending request
- POST …/resolve Approve=true: re-checks requester has no FamilyMember (race guard) + DbUpdateException catch

## Frontend

### New files
- `core/services/join-request.service.ts` — submitRequest, getMyRequest, cancelRequest
- `features/auth/pages/join-request-pending.component.ts` — locked pending screen, 30s poll

### Modified files
- `core/models/family.model.ts` — added `joinCode?` to Family, added `JoinRequestResponse`
- `core/auth/auth.service.ts` — `th_pending_request` localStorage key, routing logic
- `core/services/family.service.ts` — getJoinRequests, resolveJoinRequest
- `features/auth/pages/create-family.component.ts` — join-with-code toggle
- `features/settings/pages/family-settings.component.ts` — JoinCode display, requests modal
- `shared/components/shell.component.ts` — admin banner

### Auth routing
1. `hasFamily === true` → `/dashboard`
2. `hasPendingRequest === true` → `/join-request-pending`
3. otherwise → `/create-family`

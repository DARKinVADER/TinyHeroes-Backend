# Prize Tracking & Comments — Design Spec

**Date:** 2026-05-27

## Context

Prizes are already defined and assigned per rank (weekly: 🥇🥈🥉, monthly: 🏅) via `PrizeAssignment`. They are displayed on the podium and in history, but there is no way to track whether a prize was actually given to a child, or to leave notes about it.

This feature adds a "used" toggle and a comment thread to each prize shown in the History tab, so parents and co-parents can track real-world prize delivery and leave notes for each other.

## Data Model

### `PrizeClaims` table

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid PK | |
| `FamilyId` | Guid FK → Families | for auth checks |
| `Scope` | string(10) | `"weekly"` or `"monthly"` |
| `WeekSummaryId` | Guid? FK → WeekSummaries | set when scope = weekly |
| `MonthSummaryId` | Guid? FK → MonthSummaries | set when scope = monthly |
| `Rank` | int? | 1/2/3 weekly; null monthly |
| `ChildId` | Guid | who won |
| `ChildName` | string(100) | denormalized snapshot at award time |
| `PrizeEmoji` | string(50) | snapshot at award time |
| `PrizeLabel` | string(200) | snapshot at award time |
| `IsUsed` | bool | default false |
| `UsedAt` | DateTime? UTC | set when marked used, cleared when unmarked |
| `CreatedAt` | DateTime UTC | |

Unique constraint: `(FamilyId, Scope, WeekSummaryId, MonthSummaryId, Rank)`.

Claims are created **lazily** — a row is only inserted when a family member first interacts (marks used or adds a comment). No batch generation on summary creation.

### `PrizeComments` table

| Field | Type | Notes |
|---|---|---|
| `Id` | Guid PK | |
| `PrizeClaimId` | Guid FK → PrizeClaims | cascade delete |
| `Text` | string(1000) | |
| `CreatedAt` | DateTime UTC | |

## API

New controller: `PrizeClaimController` at `/api/prize-claims`.

| Method | Route | Auth | Notes |
|---|---|---|---|
| `GET` | `/api/prize-claims?weekSummaryId={id}` | Any family member | Returns claims + embedded comments for a week summary |
| `GET` | `/api/prize-claims?monthSummaryId={id}` | Any family member | Returns claims + embedded comments for a month summary |
| `POST` | `/api/prize-claims` | Admin or co-parent | Create claim lazily on first interaction |
| `PUT` | `/api/prize-claims/{id}/used` | Admin or co-parent | `{ isUsed: bool }` — sets/clears `IsUsed` and `UsedAt` |
| `POST` | `/api/prize-claims/{id}/comments` | Admin or co-parent | `{ text }` — adds a comment |
| `DELETE` | `/api/prize-claims/{id}/comments/{commentId}` | Admin or co-parent | Deletes a comment |

**DTOs:**
- `PrizeClaimDto`: all claim fields + `Comments: PrizeCommentDto[]`
- `PrizeCommentDto`: `Id`, `Text`, `CreatedAt`
- `CreatePrizeClaimRequest`: `Scope`, `WeekSummaryId?`, `MonthSummaryId?`, `Rank?`, `ChildId`, `ChildName`, `PrizeEmoji`, `PrizeLabel` (FamilyId derived server-side from auth)
- `UpdateUsedRequest`: `IsUsed`
- `AddCommentRequest`: `Text`

All endpoints validate that the claim belongs to the caller's family (same pattern as other controllers).

## Frontend

### New files
- `core/models/prize-claim.model.ts` — `PrizeClaim` and `PrizeComment` interfaces
- `core/services/prize-claim.service.ts` — wraps all new API endpoints

### Changes to `history.component.ts`
- After loading summaries, fetch claims in parallel: `prize-claim.service.getByWeekSummary(id)` / `getByMonthSummary(id)`
- Keep existing Rankings section unchanged
- Add a **"🎁 Prizes"** section below rankings for each summary card, showing one prize card per ranked child (up to 3 weekly, 1 monthly)

### Prize card UI (inline expansion, chosen layout)

**Collapsed state:**
- Emoji + label + child name/rank on the left — tap this area to expand
- Used badge (green "✓ Used") on the right when used; "Mark used" button on the right when pending (tapping it marks used directly, without expanding)

**Expanded state (inline below header, toggled by tapping the left area):**
- `IsUsed` toggle (checkbox + label)
- Comment list — each comment shows text + `CreatedAt` date
- "Add a comment" text input + Post button

**Lazy claim creation:**
- On first interaction (toggle used OR post comment), call `POST /api/prize-claims` with the snapshot data from the summary, then proceed with the intended action
- Cache the returned claim in the component so subsequent interactions reuse it

### Auth
Both admin and co-parent roles can toggle used and add/delete comments. The toggle and input are shown to all authenticated family members.

## Testing

### Backend integration tests (`TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs`)

Follow the existing pattern: `TestWebApplicationFactory<Program>` with EF InMemory, `TestAuthHelper` for auth.

Cover:
- `GET` returns empty list when no claims exist for a summary
- `GET` returns claims with embedded comments
- `POST` creates a claim; duplicate `POST` with same scope/summaryId/rank returns the existing claim (idempotent)
- `PUT /used` with `isUsed: true` sets `IsUsed = true` and populates `UsedAt`
- `PUT /used` with `isUsed: false` clears both fields
- `POST /comments` adds a comment; returned DTO includes the new comment
- `DELETE /comments/{id}` removes the comment; subsequent GET no longer includes it
- All write endpoints return 403 for unauthenticated requests
- All write endpoints succeed for both admin and co-parent roles
- All endpoints return 400 when `FamilyId` mismatch (claim belongs to another family)

### Playwright e2e tests (`frontend/e2e/prize-tracking.spec.ts`)

Follow existing e2e patterns in `frontend/e2e/`.

Cover:
- History tab shows "🎁 Prizes" section for a past week with prize assignments configured
- Tapping the prize card expands it inline (toggle and comment input appear)
- Tapping "Mark used" on a collapsed card marks it used without expanding; badge turns green
- Checking the toggle inside the expanded card also marks used
- Unchecking the toggle clears used status
- Typing a comment and clicking Post adds it to the list with a timestamp
- Deleting a comment removes it from the list
- Co-parent user can toggle and comment (same capabilities as admin)

## Verification

1. Run the backend: `cd backend && dotnet run --project TinyHeroes.Api`
2. Run the frontend: `cd frontend && npm start`
3. Log in as a parent (admin or co-parent) and navigate to `/podium` → History tab
4. Expand a past week — confirm the "🎁 Prizes" section appears below rankings
5. Tap a prize card — confirm it expands inline with toggle and comment input
6. Mark a prize as used — confirm the badge turns green and `UsedAt` is set
7. Add a comment — confirm it appears with a timestamp
8. Delete a comment — confirm it disappears
9. Log in as a co-parent — confirm they can also toggle and comment
10. Run backend tests: `cd backend && dotnet test`
11. Run e2e tests: `cd frontend && npm run e2e`

# Endpoint Review — Issue #36 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three categories of REST convention violations across all controllers: wrong HTTP status codes on POST, wrong HTTP verbs on update endpoints, and missing route type constraints.

**Architecture:** Pure backend refactor — no schema changes, no migrations, no new endpoints. The changes are surgical: return value corrections and attribute changes only. The frontend service calls remain compatible because Angular's `HttpClient` doesn't distinguish `200` from `201` in normal `.subscribe()` usage, and the `PUT → PATCH` changes only affect the verb the client sends. All failing tests will be fixed in the same commit as the code change.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core, xUnit 2.9.3, FluentAssertions 8.10
- Frontend: Angular 21.2 (minor `http.put` → `http.patch` call updates in two services)

---

## Full Endpoint Inventory

Legend: ⚠️ F1 = wrong POST status code · ⚠️ F2 = wrong HTTP verb · ⚠️ F3 = missing `:guid` constraint · ✓ = compliant

| Verb | Path | Notes |
|---|---|---|
| `POST` | `/api/auth/register` | ✓ returns `200` — auth token response, not a resource creation |
| `POST` | `/api/auth/login` | ✓ returns `200` — same |
| `GET` | `/api/auth/social/{provider}` | ✓ |
| `GET` | `/api/auth/social/{provider}/callback` | ✓ redirect, not a resource |
| `POST` | `/api/auth/exchange` | ✓ returns token, not a resource |
| `POST` | `/api/children` | ⚠️ F1 — returns `200`, should be `201` |
| `GET` | `/api/children` | ✓ |
| `GET` | `/api/children/{id:guid}` | ✓ |
| `PUT` | `/api/children/{id:guid}` | ✓ full replacement of a known resource by ID |
| `DELETE` | `/api/children/{id:guid}` | ✓ |
| `POST` | `/api/children/{id:guid}/avatar` | ✓ returns `200` — sub-resource update, not a new resource |
| `POST` | `/api/deeds` | ⚠️ F1 — returns `200`, should be `201` |
| `GET` | `/api/deeds` | ✓ |
| `GET` | `/api/deeds/stats` | ✓ |
| `POST` | `/api/deeds/generate-image` | ✓ returns `200` — action, not a persisted resource |
| `POST` | `/api/families` | ⚠️ F1 — returns `200`, should be `201` |
| `GET` | `/api/families/mine` | ✓ |
| `PATCH` | `/api/families/mine` | ✓ |
| `PATCH` | `/api/families/mine/prize-rules` | ✓ |
| `DELETE` | `/api/families/mine/members/{memberId:guid}` | ✓ |
| `DELETE` | `/api/families/mine` | ✓ |
| `GET` | `/api/families/join-requests` | ✓ admin view of incoming requests |
| `POST` | `/api/families/join-requests/{id:guid}/resolve` | ✓ action endpoint |
| `GET` | `/api/info` | ✓ |
| `POST` | `/api/invites` | ⚠️ F1 — returns `200`, should be `201` |
| `POST` | `/api/invites/{token}/accept` | ✓ returns `200` — action, not a resource creation |
| `POST` | `/api/join-requests` | ✓ already uses `CreatedAtAction` → `201` |
| `GET` | `/api/join-requests` | ✓ singleton — user's own pending request |
| `DELETE` | `/api/join-requests` | ✓ |
| `GET` | `/api/presets` | ✓ |
| `POST` | `/api/presets` | ✓ already uses `CreatedAtAction` → `201` |
| `DELETE` | `/api/presets/{id:guid}` | ✓ |
| `GET` | `/api/prize-assignments` | ✓ |
| `PUT` | `/api/prize-assignments` | ⚠️ F2 — upsert with no ID in URL, should be `PATCH` |
| `GET` | `/api/prize-claims` | ✓ |
| `POST` | `/api/prize-claims` | ✓ already uses `CreatedAtAction` → `201` |
| `PUT` | `/api/prize-claims/{id}/used` | ⚠️ F2 F3 — partial field update, should be `PATCH`; `{id}` missing `:guid` |
| `POST` | `/api/prize-claims/{id}/comments` | ⚠️ F3 — `{id}` missing `:guid` |
| `DELETE` | `/api/prize-claims/{id}/comments/{commentId}` | ⚠️ F3 — `{id}` and `{commentId}` both missing `:guid` |
| `GET` | `/api/prize-presets` | ✓ |
| `POST` | `/api/prize-presets` | ✓ already uses `CreatedAtAction` → `201` |
| `DELETE` | `/api/prize-presets/{id:guid}` | ✓ |
| `GET` | `/api/summaries/weeks` | ✓ |
| `GET` | `/api/summaries/months` | ✓ |
| `GET` | `/api/summaries/current-month` | ✓ |
| `GET` | `/api/users/me` | ✓ |
| `PATCH` | `/api/users/me` | ✓ |

**Summary: 35 compliant · 4× F1 · 2× F2 · 3× F3**

---

## Findings

### F1 — POST endpoints returning 200 instead of 201

REST convention: creating a resource returns `201 Created`, not `200 OK`. The `Location` header is optional for internal APIs, but the status code is observable by clients and tests.

| Controller | Endpoint | Current | Should be |
|---|---|---|---|
| `FamilyController` | `POST /api/families` | `200 OK` | `201 Created` |
| `ChildController` | `POST /api/children` | `200 OK` | `201 Created` |
| `ChildController` | `POST /api/children/{id}/avatar` | `200 OK` | `200 OK` ✓ (update, not create) |
| `DeedController` | `POST /api/deeds` | `200 OK` | `201 Created` |
| `InviteController` | `POST /api/invites` | `200 OK` | `201 Created` |
| `PrizeClaimController` | `POST /api/prize-claims` | already uses `CreatedAtAction` ✓ | — |
| `JoinRequestController` | `POST /api/join-requests` | already uses `CreatedAtAction` ✓ | — |

### F2 — PUT used for partial update / upsert

`PUT` means full replacement of a **known, addressable** resource. Two endpoints misuse it:

| Controller | Endpoint | Problem | Fix |
|---|---|---|---|
| `PrizeAssignmentController` | `PUT /api/prize-assignments` | Upsert (create or update) with no resource ID in the URL | Change to `PATCH /api/prize-assignments` — partial update semantics, idempotent, no ID required when the resource is identified by its (familyId + scope + rank) composite key |
| `PrizeClaimController` | `PUT /api/prize-claims/{id}/used` | Sets one field (`isUsed`) — this is a partial update | Change to `PATCH /api/prize-claims/{id}/used` |

### F3 — Missing `:guid` route constraints in PrizeClaimController

`ChildController`, `PresetController`, etc. consistently use `{id:guid}`. `PrizeClaimController` uses bare `{id}` throughout, meaning a non-GUID string routes to the action instead of returning `404` from the router.

| Route | Current | Fix |
|---|---|---|
| `PUT {id}/used` | `{id}` | `{id:guid}` |
| `POST {id}/comments` | `{id}` | `{id:guid}` |
| `DELETE {id}/comments/{commentId}` | `{id}`, `{commentId}` | `{id:guid}`, `{commentId:guid}` |

### F4 — `mine` qualifier on `/api/families/mine/*` (no action needed — intentional)

The `mine` qualifier exists because `POST /api/families` occupies the collection root, making a bare `GET /api/families` ambiguous. The singular alternative (`GET /api/family`) would be cleaner today but is a dead-end: issue #37 tracks multi-family membership (grandparent access), which requires replacing `mine` with an explicit `{id}` anyway. Refactoring to singular now and then to `{id}` later is churn with no user value. `mine` stays until #37 lands.

### F5 — join-requests routing split (no action needed — intentional design)

`JoinRequestController` at `/api/join-requests` handles the **requester's** own pending request (submit, view, cancel). `FamilyController` at `/api/families/join-requests` handles the **admin's** view of incoming requests. This split is intentional: the requester has no family membership yet and cannot be scoped under `/families/mine`. **No change needed.**

---

## File Map

| File | Action |
|---|---|
| `backend/TinyHeroes.Api/Controllers/FamilyController.cs` | Modify — `Create`: `Ok(...)` → `CreatedAtAction(...)` |
| `backend/TinyHeroes.Api/Controllers/ChildController.cs` | Modify — `Create`: `Ok(...)` → `CreatedAtAction(...)` |
| `backend/TinyHeroes.Api/Controllers/DeedController.cs` | Modify — `Create`: `Ok(...)` → `CreatedAtAction(...)` |
| `backend/TinyHeroes.Api/Controllers/InviteController.cs` | Modify — `Create`: `Ok(...)` → `CreatedAtAction(...)` |
| `backend/TinyHeroes.Api/Controllers/PrizeAssignmentController.cs` | Modify — `[HttpPut]` → `[HttpPatch]` |
| `backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs` | Modify — `[HttpPut("{id}/used")]` → `[HttpPatch("{id:guid}/used")]`, add `:guid` constraints |
| `backend/TinyHeroes.Tests/Integration/FamilySettingsControllerTests.cs` | Modify — assert `201` on family create |
| `backend/TinyHeroes.Tests/Integration/ChildControllerTests.cs` | Modify — assert `201` on child create |
| `backend/TinyHeroes.Tests/Integration/DeedControllerTests.cs` | Modify — assert `201` on deed create |
| `backend/TinyHeroes.Tests/Integration/InviteControllerTests.cs` | Modify — assert `201` on invite create |
| `backend/TinyHeroes.Tests/Integration/PrizeAssignmentControllerTests.cs` | Modify — assert verb change (PUT → PATCH) |
| `backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs` | Modify — assert `PATCH` on `/used` |
| `frontend/src/app/core/services/prize.service.ts` | Modify — `http.put` → `http.patch` |
| `frontend/src/app/core/services/prize-claim.service.ts` | Modify — `http.put` → `http.patch` on `/used` |
| `frontend/src/environments/environment.ts` | Modify — bump PATCH version |
| `frontend/src/environments/environment.prod.ts` | Modify — bump PATCH version |
| `CHANGELOG.md` | Modify — add entry under `## [Unreleased]` |

---

## Task 1: Create feature branch

- [ ] **Step 1: Create branch off master**

```bash
git checkout master && git pull
git checkout -b chore/36-endpoint-review
```

---

## Task 2: Fix POST status codes — FamilyController, ChildController, DeedController, InviteController

**Files:**
- Modify: `backend/TinyHeroes.Api/Controllers/FamilyController.cs`
- Modify: `backend/TinyHeroes.Api/Controllers/ChildController.cs`
- Modify: `backend/TinyHeroes.Api/Controllers/DeedController.cs`
- Modify: `backend/TinyHeroes.Api/Controllers/InviteController.cs`

`CreatedAtAction(actionName, routeValues, value)` requires an action name that exists in the same controller. Use `nameof(GetMine)` for family, `nameof(Get)` for child, `nameof(List)` for deed, and `nameof(Accept)` for invite. When there is no single-resource GET in the controller, use `CreatedAtAction(null, null, value)` — ASP.NET Core will still emit `201` and omit the `Location` header.

- [ ] **Step 1: Update FamilyController.Create** — change the final `return Ok(...)` to:

```csharp
return CreatedAtAction(nameof(GetMine), new FamilyResponse(family.Id, family.Name, family.WeekStartDay, family.WeeklyMinDeeds, family.MonthlyMinDeeds, family.JoinCode));
```

- [ ] **Step 2: Update ChildController.Create** (`backend/TinyHeroes.Api/Controllers/ChildController.cs`, ~line 38) — change `return Ok(...)` to:

```csharp
return CreatedAtAction(nameof(Get), new { id = child.Id }, new ChildResponse(child.Id, child.Name, child.Age, child.Gender, child.AvatarEmoji, child.AvatarUrl));
```

- [ ] **Step 3: Update DeedController.Create** (`backend/TinyHeroes.Api/Controllers/DeedController.cs`, ~line 33) — change `return Ok(...)` to:

```csharp
return CreatedAtAction(nameof(List), new { childId = deed.ChildId }, new DeedResponse(deed.Id, deed.ChildId, deed.Description, deed.ImageType, deed.ImageValue, user!.DisplayName, deed.CreatedAt));
```

- [ ] **Step 4: Update InviteController.Create** (`backend/TinyHeroes.Api/Controllers/InviteController.cs`, ~line 39) — change `return Ok(...)` to:

```csharp
return CreatedAtAction(nameof(Accept), new { token = invite.Token }, new InviteResponse(invite.Id, invite.Token, invite.Email, invite.ExpiresAt));
```

- [ ] **Step 5: Build to verify no compile errors**

```bash
cd backend && dotnet build TinyHeroes.Api
```
Expected: `Build succeeded.`

---

## Task 3: Update tests for 201 status codes

**Files:**
- Modify: `backend/TinyHeroes.Tests/Integration/FamilySettingsControllerTests.cs`
- Modify: `backend/TinyHeroes.Tests/Integration/ChildControllerTests.cs`
- Modify: `backend/TinyHeroes.Tests/Integration/DeedControllerTests.cs`
- Modify: `backend/TinyHeroes.Tests/Integration/InviteControllerTests.cs`

Find assertions in the happy-path create tests. Most use `response.StatusCode.Should().Be(HttpStatusCode.OK)` after a POST. Change those assertions to `HttpStatusCode.Created`.

- [ ] **Step 1: Search for POST create assertions to update**

```bash
grep -n "HttpStatusCode.OK\|StatusCodes.Status200OK" \
  backend/TinyHeroes.Tests/Integration/FamilySettingsControllerTests.cs \
  backend/TinyHeroes.Tests/Integration/ChildControllerTests.cs \
  backend/TinyHeroes.Tests/Integration/DeedControllerTests.cs \
  backend/TinyHeroes.Tests/Integration/InviteControllerTests.cs
```

For each line that corresponds to a POST create test, change `HttpStatusCode.OK` → `HttpStatusCode.Created`.

Note: `FamilySettingsControllerTests.cs` calls `RegisterWithFamily` via `TestAuthHelper`, which calls `POST /api/families` internally. That helper reads the response body, not the status code — no test assertion change needed there. Check `FamilySettingsControllerTests.cs` for any explicit `POST /api/families` assertions and update only those.

- [ ] **Step 2: Run the affected test classes**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~FamilySettingsControllerTests|FullyQualifiedName~ChildControllerTests|FullyQualifiedName~DeedControllerTests|FullyQualifiedName~InviteControllerTests"
```
Expected: all PASS.

- [ ] **Step 3: Commit**

```bash
git add \
  backend/TinyHeroes.Api/Controllers/FamilyController.cs \
  backend/TinyHeroes.Api/Controllers/ChildController.cs \
  backend/TinyHeroes.Api/Controllers/DeedController.cs \
  backend/TinyHeroes.Api/Controllers/InviteController.cs \
  backend/TinyHeroes.Tests/Integration/FamilySettingsControllerTests.cs \
  backend/TinyHeroes.Tests/Integration/ChildControllerTests.cs \
  backend/TinyHeroes.Tests/Integration/DeedControllerTests.cs \
  backend/TinyHeroes.Tests/Integration/InviteControllerTests.cs
git commit -m "fix: POST create endpoints now return 201 Created"
```

---

## Task 4: Fix verb misuse — PrizeAssignmentController PUT → PATCH

**File:** `backend/TinyHeroes.Api/Controllers/PrizeAssignmentController.cs`

`PUT /api/prize-assignments` is an upsert identified by `(familyId + scope + rank)` from the body — not a full replacement of a known resource URL. `PATCH` is the correct verb.

- [ ] **Step 1: Change `[HttpPut]` to `[HttpPatch]`** at line 34

```csharp
[HttpPatch]
public async Task<ActionResult<PrizeAssignmentResponse>> Set(SetPrizeRequest req)
```

- [ ] **Step 2: Update the test**

In `backend/TinyHeroes.Tests/Integration/PrizeAssignmentControllerTests.cs`, find every call to `PutAsJsonAsync("/api/prize-assignments", ...)` and change it to `PatchAsJsonAsync("/api/prize-assignments", ...)`.

```bash
grep -n "PutAsJsonAsync\|put.*prize-assignment" backend/TinyHeroes.Tests/Integration/PrizeAssignmentControllerTests.cs
```

Replace each `PutAsJsonAsync` with `PatchAsJsonAsync`.

- [ ] **Step 3: Run the test class**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~PrizeAssignmentControllerTests"
```
Expected: all PASS.

- [ ] **Step 4: Update the frontend service**

In `frontend/src/app/core/services/prize.service.ts`, find:

```typescript
return this.http.put<PrizeAssignmentDto>(`${environment.apiUrl}/prize-assignments`, req);
```

Change to:

```typescript
return this.http.patch<PrizeAssignmentDto>(`${environment.apiUrl}/prize-assignments`, req);
```

- [ ] **Step 5: Verify TypeScript compiles**

```bash
cd frontend && npx ng build --configuration production 2>&1 | grep "error TS"
```
Expected: no output.

- [ ] **Step 6: Commit**

```bash
git add \
  backend/TinyHeroes.Api/Controllers/PrizeAssignmentController.cs \
  backend/TinyHeroes.Tests/Integration/PrizeAssignmentControllerTests.cs \
  frontend/src/app/core/services/prize.service.ts
git commit -m "fix: PUT /prize-assignments is a partial upsert, use PATCH"
```

---

## Task 5: Fix verb misuse and route constraints in PrizeClaimController

**File:** `backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs`

Two fixes in one file: `PUT → PATCH` on the `/used` sub-resource (partial field update), and `:guid` constraints on all three routes that take an `{id}`.

- [ ] **Step 1: Apply both fixes**

Change the three attributes:

```csharp
// before
[HttpPut("{id}/used")]
[HttpPost("{id}/comments")]
[HttpDelete("{id}/comments/{commentId}")]

// after
[HttpPatch("{id:guid}/used")]
[HttpPost("{id:guid}/comments")]
[HttpDelete("{id:guid}/comments/{commentId:guid}")]
```

The method parameters stay `Guid id` / `Guid commentId` — the `:guid` constraint just moves validation to the router layer so malformed IDs get `404` from routing, not a runtime cast error.

- [ ] **Step 2: Update the test**

In `backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs`, find calls to `PutAsJsonAsync(...)` on a `/used` URL and change to `PatchAsJsonAsync(...)`.

```bash
grep -n "PutAsJsonAsync\|put.*prize-claim\|/used" backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs
```

- [ ] **Step 3: Run the test class**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~PrizeClaimControllerTests"
```
Expected: all PASS.

- [ ] **Step 4: Update the frontend service**

In `frontend/src/app/core/services/prize-claim.service.ts`, find the call that hits `/{id}/used`:

```bash
grep -n "put\|used" frontend/src/app/core/services/prize-claim.service.ts
```

Change `this.http.put<...>(... /used ...)` to `this.http.patch<...>(... /used ...)`.

- [ ] **Step 5: Verify TypeScript compiles**

```bash
cd frontend && npx ng build --configuration production 2>&1 | grep "error TS"
```
Expected: no output.

- [ ] **Step 6: Commit**

```bash
git add \
  backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs \
  backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs \
  frontend/src/app/core/services/prize-claim.service.ts
git commit -m "fix: PATCH for partial update, add :guid route constraints in PrizeClaimController"
```

---

## Task 6: Run full test suite

- [ ] **Step 1: Run all backend tests**

```bash
cd backend && dotnet test
```
Expected: all PASS. If anything fails, investigate before continuing — a test failure here means a regression introduced in Tasks 2–5.

---

## Task 7: Docs, CHANGELOG, version bump

- [ ] **Step 1: Update `CHANGELOG.md` under `## [Unreleased]`**

```markdown
### Fixed
- POST create endpoints (`/api/families`, `/api/children`, `/api/deeds`, `/api/invites`) now return `201 Created` instead of `200 OK`.
- `PUT /api/prize-assignments` changed to `PATCH` — the endpoint is an upsert, not a full resource replacement.
- `PUT /api/prize-claims/{id}/used` changed to `PATCH` — the endpoint sets one field, making it a partial update.
- Added `:guid` route constraints to `PrizeClaimController` routes (`{id}`, `{commentId}`) so malformed IDs return `404` from the router.
```

- [ ] **Step 2: Bump frontend PATCH version**

Current version is `2.4.2`. Bump to `2.4.3` in:
- `frontend/src/environments/environment.ts` — `version: '2.4.3'`
- `frontend/src/environments/environment.prod.ts` — `version: '2.4.3'`

- [ ] **Step 3: Commit**

```bash
git add \
  CHANGELOG.md \
  frontend/src/environments/environment.ts \
  frontend/src/environments/environment.prod.ts
git commit -m "chore: bump frontend version to 2.4.3, update changelog for endpoint review"
```

---

## Task 8: Post-feature review, push, PR

- [ ] **Step 1: Run code review**

```
/code-review
```
Address all findings.

- [ ] **Step 2: Run security review**

```
/security-review
```
Address all findings.

- [ ] **Step 3: Push branch**

```bash
git push -u origin chore/36-endpoint-review
```

- [ ] **Step 4: Open PR**

```bash
gh pr create --base master \
  --title "chore: endpoint review — status codes, verbs, route constraints" \
  --body "Closes #36

## Summary
- POST create endpoints return \`201 Created\` (was \`200 OK\`)
- \`PUT /prize-assignments\` and \`PUT /prize-claims/{id}/used\` changed to \`PATCH\` (partial update semantics)
- \`:guid\` route constraints added to \`PrizeClaimController\`

## No breaking changes
- Frontend services updated in the same PR (\`http.put → http.patch\` in \`prize.service.ts\` and \`prize-claim.service.ts\`)
- Angular's \`HttpClient\` treats \`201\` and \`200\` identically in \`.subscribe()\`

## Test plan
- [ ] \`dotnet test\` — all tests pass
- [ ] On integration env (\`testuser@demo.com\`): open Podium → confirm week/month summaries load
- [ ] Go to Settings → Prize Rules → save a prize → no console errors
- [ ] Dashboard → add a deed → confirm it appears in the deed list"
```

- [ ] **Step 5: Update project card to In Review**

```bash
gh project item-list 5 --owner DARKinVADER --format json \
  | jq '.items[] | select(.content.number == 36) | .id'

gh project item-edit \
  --project-id PVT_kwHOACNlms4BZWnC \
  --id <PVTI_from_above> \
  --field-id PVTSSF_lAHOACNlms4BZWnCzhUV0xU \
  --single-select-option-id b08d4b47
```

---

## Verification

End-to-end on `https://integration.mytinyheroes.net` (credentials: `testuser@demo.com / Password1!`):

1. Open browser DevTools → Network tab
2. Create a deed → confirm the `POST /api/deeds` response shows status `201`
3. Open Settings → Family → Prize Rules → save a prize → confirm `PATCH /api/prize-assignments` (was `PUT`) shows `200`
4. Open Podium → confirm week and month summaries load without errors
5. Open a prize claim → confirm marking as used sends `PATCH` (was `PUT`)

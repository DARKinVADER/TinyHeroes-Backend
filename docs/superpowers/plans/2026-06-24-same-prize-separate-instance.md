# Same Prize Separate Instance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow multiple children who tie for the same rank to get distinct prize claim instances on both backend API and frontend history view.

**Architecture:** Include `ChildId` in the uniqueness check for claims in the backend, and modify the frontend history component to track, expand, and comment on claims using both rank and child ID.

**Tech Stack:** .NET 10, EF Core, Angular 17

---

## Proposed Changes

### Backend

#### [MODIFY] [PrizeClaimController.cs](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs)
Update the `Create` query to match on `req.ChildId` in addition to rank, scope, and summaries:
- Add `c.ChildId == req.ChildId` to the `FirstOrDefaultAsync` condition.

#### [MODIFY] [PrizeClaimControllerTests.cs](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs)
Add a test `CreateClaim_WithTiedRank_CreatesSeparateClaims` to verify that when two claims with the same rank and summary but different child IDs are submitted, both are created (returns HTTP 201).

---

### Frontend

#### [MODIFY] [history.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/podium/pages/history.component.ts)
Update component logic to support separate claims for tied children:
- Update `getWeekPrizeEntries` to include `childId` in the returned object.
- Update `findClaim` to filter by `childId`.
- Update `@for` loop track key, expansion keys, and comment drafts keys to include `entry.childId`.
- Update `summaryIdFromKey` to extract the first 36 characters of the key.
- Update quickMarkUsed, postComment, and deleteComment to accept the new entry signature.

---

### Version Bumps & CHANGELOG

#### [MODIFY] [CHANGELOG.md](file:///Volumes/PersonalProtected/GIT/TinyHeroes/CHANGELOG.md)
Add user-facing bug fix description under `[Unreleased]`.

#### [MODIFY] [TinyHeroes.Api.csproj](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Api/TinyHeroes.Api.csproj)
Bump backend version from `1.5.2` to `1.5.3`.

#### [MODIFY] [environment.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/environments/environment.ts)
Bump frontend version from `3.4.0` to `3.4.1`.

#### [MODIFY] [environment.prod.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/environments/environment.prod.ts)
Bump frontend version from `3.4.0` to `3.4.1`.

#### [MODIFY] [environment.integration.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/environments/environment.integration.ts)
Bump frontend version from `3.4.0` to `3.4.1`.

---

## Tasks

### Task 1: Backend Implementation

**Files:**
- Modify: `backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs`
- Modify: `backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs`

- [ ] **Step 1: Write the failing integration test**
  Add `CreateClaim_WithTiedRank_CreatesSeparateClaims` in [PrizeClaimControllerTests.cs](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs):
  ```csharp
      [Fact]
      public async Task CreateClaim_WithTiedRank_CreatesSeparateClaims()
      {
          var client = await TestAuthHelper.RegisterWithFamily(factory);
          var summaryId = Guid.NewGuid();
          var child1Id = Guid.NewGuid();
          var child2Id = Guid.NewGuid();

          var req1 = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, child1Id, "Alice", "🍕", "Pizza night");
          var req2 = new CreatePrizeClaimRequest("weekly", summaryId, null, 1, child2Id, "Bob", "🍕", "Pizza night");

          var first = await client.PostAsJsonAsync("/api/prize-claims", req1);
          var second = await client.PostAsJsonAsync("/api/prize-claims", req2);

          first.StatusCode.Should().Be(HttpStatusCode.Created);
          second.StatusCode.Should().Be(HttpStatusCode.Created);

          var getResponse = await client.GetAsync($"/api/prize-claims?weekSummaryId={summaryId}");
          getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
          var claims = await getResponse.Content.ReadFromJsonAsync<List<PrizeClaimDto>>(TestWebApplicationFactory<Program>.JsonOptions);
          claims.Should().HaveCount(2);
          claims.Should().ContainSingle(c => c.ChildId == child1Id);
          claims.Should().ContainSingle(c => c.ChildId == child2Id);
      }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `cd backend && dotnet test --filter "FullyQualifiedName~PrizeClaimControllerTests.CreateClaim_WithTiedRank_CreatesSeparateClaims"`
  Expected: FAIL (second claim fails with 200 OK and duplicates first claim).

- [ ] **Step 3: Modify backend controller**
  Update the query in [PrizeClaimController.cs](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs):
  ```csharp
          var existing = await db.PrizeClaims
              .Include(c => c.Comments)
              .FirstOrDefaultAsync(c =>
                  c.FamilyId == membership.FamilyId &&
                  c.Scope == req.Scope &&
                  c.WeekSummaryId == req.WeekSummaryId &&
                  c.MonthSummaryId == req.MonthSummaryId &&
                  c.Rank == req.Rank &&
                  c.ChildId == req.ChildId);
  ```

- [ ] **Step 4: Run test to verify it passes**
  Run: `cd backend && dotnet test`
  Expected: PASS

- [ ] **Step 5: Commit backend changes**
  Run:
  ```bash
  git add backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs
  git commit -m "fix(api): support separate prize claim instances for tied ranks"
  ```

---

### Task 2: Frontend Implementation

**Files:**
- Modify: `frontend/src/app/features/podium/pages/history.component.ts`

- [ ] **Step 1: Update frontend type and methods**
  Modify [history.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/podium/pages/history.component.ts):
  
  Replace `getWeekPrizeEntries` implementation:
  ```typescript
    getWeekPrizeEntries(week: WeekSummaryDto): { rank: number; assignment: PrizeAssignmentDto; childId: string; childName: string }[] {
      return this.topPodium(week.rankings)
        .map(entry => {
          const assignment = this.prizeService.assignments().find(a => a.scope === 'weekly' && a.rank === entry.rank);
          if (!assignment) return null;
          return { rank: entry.rank, assignment, childId: entry.childId, childName: entry.childName };
        })
        .filter((x): x is { rank: number; assignment: PrizeAssignmentDto; childId: string; childName: string } => x !== null);
    }
  ```

  Replace `findClaim` implementation:
  ```typescript
    findClaim(claims: PrizeClaimDto[], rank: number | null, childId?: string): PrizeClaimDto | undefined {
      return claims.find(c => c.rank === rank && (!childId || c.childId === childId));
    }
  ```

  Replace interaction methods:
  ```typescript
    async quickMarkUsed(week: WeekSummaryDto, entry: { rank: number; assignment: PrizeAssignmentDto; childId: string; childName: string }) {
      this.saving.set(true);
      try {
        const claim = await this.getOrCreateClaim(week.id, 'weekly', week.id, null, entry.rank,
          entry.childId, entry.childName, entry.assignment.emoji, entry.assignment.label);
        this.prizeClaimService.setUsed(claim.id, true).subscribe(updated => this.updateClaimInMap(week.id, updated));
      } finally { this.saving.set(false); }
    }

    async postComment(week: WeekSummaryDto, entry: { rank: number; assignment: PrizeAssignmentDto; childId: string; childName: string }) {
      const key = week.id + '-' + entry.rank + '-' + entry.childId;
      const text = this.commentDraft[key]?.trim();
      if (!text) return;
      this.saving.set(true);
      try {
        const claim = await this.getOrCreateClaim(week.id, 'weekly', week.id, null, entry.rank,
          entry.childId, entry.childName, entry.assignment.emoji, entry.assignment.label);
        this.prizeClaimService.addComment(claim.id, text).subscribe(comment => {
          this.updateClaimInMap(week.id, { ...claim, comments: [...claim.comments, comment] });
          this.commentDraft[key] = '';
        });
      } finally { this.saving.set(false); }
    }

    async deleteComment(week: WeekSummaryDto, entry: { rank: number; assignment: PrizeAssignmentDto; childId: string; childName: string }, commentId: string) {
      const claim = this.findClaim(this.getClaimsForSummary(week.id), entry.rank, entry.childId);
      if (!claim) return;
      this.saving.set(true);
      this.prizeClaimService.deleteComment(claim.id, commentId).subscribe(() => {
        this.updateClaimInMap(week.id, { ...claim, comments: claim.comments.filter(c => c.id !== commentId) });
        this.saving.set(false);
      });
    }
  ```

  Replace `summaryIdFromKey`:
  ```typescript
    private summaryIdFromKey(key: string): string { return key.substring(0, 36); }
  ```

  Update the inline component template in [history.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/podium/pages/history.component.ts) to:
  1. Track items in `@for` by `entry.rank + '-' + entry.childId`.
  2. Pass `entry.childId` into `findClaim(weekClaims, entry.rank, entry.childId)`.
  3. Toggle expansion using `week.id + '-' + entry.rank + '-' + entry.childId`.
  4. Bind the model to `commentDraft[week.id + '-' + entry.rank + '-' + entry.childId]`.

- [ ] **Step 2: Run frontend tests**
  Run: `cd frontend && npx ng test --watch=false`
  Expected: PASS

- [ ] **Step 3: Commit frontend changes**
  Run:
  ```bash
  git add frontend/src/app/features/podium/pages/history.component.ts
  git commit -m "fix(frontend): support separate prize claims for ties in history view"
  ```

---

### Task 3: Documentation and Version Bump

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`
- Modify: `frontend/src/environments/environment.integration.ts`
- Modify: `backend/TinyHeroes.Api/TinyHeroes.Api.csproj`

- [ ] **Step 1: Update CHANGELOG.md**
  Add a bug fix entry under `## [Unreleased]`:
  ```markdown
  ### Fixed
  - Tied children now correctly receive their own separate instances of the same won prize, allowing parents to track and comment on them individually.
  ```

- [ ] **Step 2: Update environment files for frontend**
  Change version `3.4.0` to `3.4.1` in:
  - `frontend/src/environments/environment.ts`
  - `frontend/src/environments/environment.prod.ts`
  - `frontend/src/environments/environment.integration.ts`

- [ ] **Step 3: Update csproj for backend**
  Change `<Version>1.5.2</Version>` to `<Version>1.5.3</Version>` and `<InformationalVersion>1.5.2</InformationalVersion>` to `<InformationalVersion>1.5.3</InformationalVersion>` in `backend/TinyHeroes.Api/TinyHeroes.Api.csproj`.

- [ ] **Step 4: Commit Documentation and Version Bump**
  Run:
  ```bash
  git add CHANGELOG.md frontend/src/environments/environment.ts frontend/src/environments/environment.prod.ts frontend/src/environments/environment.integration.ts backend/TinyHeroes.Api/TinyHeroes.Api.csproj
  git commit -m "chore: bump version to API v1.5.3 and frontend v3.4.1"
  ```

---

## Verification Plan

### Automated Tests
- Run `cd backend && dotnet test`
- Run `cd frontend && npx ng test --watch=false`

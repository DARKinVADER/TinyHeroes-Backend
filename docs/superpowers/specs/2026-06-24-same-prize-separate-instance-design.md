# Spec: Same Prize Separate Instance

Ensure that when multiple children tie for a rank, they each receive distinct prize claim instances. This document details the changes in both the C# backend API and Angular frontend.

## Problem Statement
When children have tied deed counts for a week, they finish at the same rank (e.g., both 1st place). Currently, the backend API matches an existing `PrizeClaim` based on `FamilyId`, `Scope`, `WeekSummaryId`, `MonthSummaryId`, and `Rank`. If child A gets the prize, when the system attempts to create a claim for child B, the backend matches on the same rank and reuses child A's `PrizeClaim` record instead of creating a new one.

In the frontend UI, claims are retrieved and tracked by rank. Under ties, this causes duplicate key issues, shared comments, and simultaneous expansion and used status toggling.

## Proposed Changes

### Backend

#### [MODIFY] [PrizeClaimController.cs](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Api/Controllers/PrizeClaimController.cs)
* In the `Create` method, update the query that looks up an existing claim.
* Add `c.ChildId == req.ChildId` to the `FirstOrDefaultAsync` condition so that each child gets their own distinct claim record.

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

### Frontend

#### [MODIFY] [history.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/podium/pages/history.component.ts)
* Update `getWeekPrizeEntries` to include `childId` in the returned object:
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
* Update `findClaim` to support an optional `childId` filter:
  ```typescript
  findClaim(claims: PrizeClaimDto[], rank: number | null, childId?: string): PrizeClaimDto | undefined {
    return claims.find(c => c.rank === rank && (!childId || c.childId === childId));
  }
  ```
* Update the inline component template and logic:
  * In the weekly prizes loop, track the entry using a combination of rank and child ID: `@for (entry of weekPrizes; track entry.rank + '-' + entry.childId)` (or just `entry.childId`).
  * Modify `@let claim = findClaim(weekClaims, entry.rank);` to pass `entry.childId`: `@let claim = findClaim(weekClaims, entry.rank, entry.childId);`.
  * Update expansion toggle key from `week.id + '-' + entry.rank` to `week.id + '-' + entry.rank + '-' + entry.childId`.
  * Update the text input model binding for comment drafts to use the new unique key: `commentDraft[week.id + '-' + entry.rank + '-' + entry.childId]`.
  * Update the parameters passed to `postComment`, `deleteComment`, and `quickMarkUsed` (they should accept the updated entry containing `childId` and `childName`).
* Update `summaryIdFromKey` to extract the 36-character GUID for the summary ID, as the key will now also contain the child ID GUID:
  ```typescript
  private summaryIdFromKey(key: string): string {
    return key.substring(0, 36);
  }
  ```

## Verification Plan

### Automated Tests
* **Backend:** Add a new integration test `CreateClaim_WithTiedRank_CreatesSeparateClaims` in [PrizeClaimControllerTests.cs](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Tests/Integration/PrizeClaimControllerTests.cs) that verifies:
  1. Creating two weekly claims for the same summary ID and rank, but different children.
  2. The second claim returns `Created` status rather than `OK` (idempotent duplicate).
  3. The database contains two distinct claims with different `ChildId`s.
  4. Run `cd backend && dotnet test` to verify.
* **Frontend:** Run `cd frontend && npx ng test --watch=false` to verify that existing component tests pass.

### Manual Verification
* Simulate a tied week where two children finish with equal deeds.
* Ensure both children are displayed in the weekly prizes section.
* Expand one child's prize card, add a comment, and toggle "Mark Used". Verify that the other child's prize card remains unaffected.

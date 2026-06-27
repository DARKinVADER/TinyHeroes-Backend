# Dense Ranking Design for In-Family Competitions

This specification outlines the transition from Standard Competition Ranking (where ties skip ranks, e.g., 1, 1, 3) to Dense Ranking (where ties do not skip ranks, e.g., 1, 1, 2) in both the backend database summaries and the frontend live calculations.

## User Review Required

> [!IMPORTANT]
> The ranking logic changes affect newly generated database summaries and live frontend/backend queries.
>
> **Design Decision**: Existing historical summaries stored in the database as pre-serialized JSON will remain untouched. Only new summaries generated going forward, and live in-progress rankings, will use Dense Ranking.


## Proposed Changes

### Backend Component

We will modify the core ranking calculation inside the backend helper class to use Dense Ranking.

#### [MODIFY] [RankingHelper.cs](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Application/Helpers/RankingHelper.cs)

We will rewrite the single-pass ranking loop to:
1. Track the current rank starting from `0`.
2. Check if the deed count of the child has changed relative to the previous child using `int? previousCount` to avoid arbitrary default or sentinel values.
3. If it has changed (or if it is the first entry), increment the rank by `1`.
4. Return the computed dense rank.

### Frontend Component

We will modify the live ranking calculation on the frontend page to align with the backend's dense ranking behavior.

#### [MODIFY] [podium.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/podium/pages/podium.component.ts)

We will update the computed `allRankings` signal's mapping loop:
- Instead of using `rank = i + 1` (Standard Competition Ranking), we will use procedural dense ranking logic matching the backend implementation.

## Verification Plan

### Automated Tests
- We will add a new test case in `SummaryServiceTests.cs` specifically verifying that tied children receive the same rank and that the subsequent child receives the next consecutive rank (e.g. `1, 1, 2`).
- We will execute `dotnet test` to verify all unit/integration tests continue to pass.

### Manual Verification
- We will review the frontend code implementation to ensure it mirrors the backend logic.

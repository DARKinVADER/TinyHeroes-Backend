# Dense Ranking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transition the in-family competition rankings from Standard Competition Ranking to Dense Ranking so that tied rankings do not skip subsequent ranks (e.g. 1st, 1st, 2nd instead of 1st, 1st, 3rd).

**Architecture:** Update the pure-functional `RankingHelper.Rank` in the backend and the computed `allRankings` signal mapping logic in the frontend to procedural Dense Ranking.

**Tech Stack:** C# (.NET 10), Angular 21, xUnit, FluentAssertions

---

### Task 1: Backend Unit Tests (TDD)

**Files:**
- Create: [RankingHelperTests.cs](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Tests/Unit/RankingHelperTests.cs)

- [ ] **Step 1: Write the failing tests**
  Create the unit test file `backend/TinyHeroes.Tests/Unit/RankingHelperTests.cs` with the following contents:
  ```csharp
  using FluentAssertions;
  using TinyHeroes.Application.Helpers;

  namespace TinyHeroes.Tests.Unit;

  public class RankingHelperTests
  {
      [Fact]
      public void Rank_EmptyCollection_ReturnsEmptyList()
      {
          var items = new List<(Guid Id, string Name, int DeedCount)>();
          var result = RankingHelper.Rank(items);
          result.Should().BeEmpty();
      }

      [Fact]
      public void Rank_SingleItem_ReturnsRankOne()
      {
          var aliceId = Guid.NewGuid();
          var items = new List<(Guid Id, string Name, int DeedCount)>
          {
              (aliceId, "Alice", 5)
          };
          var result = RankingHelper.Rank(items);
          result.Should().HaveCount(1);
          result[0].Rank.Should().Be(1);
          result[0].ChildId.Should().Be(aliceId);
      }

      [Fact]
      public void Rank_TiedItems_ReturnsDenseRankings()
      {
          var aliceId = Guid.NewGuid();
          var bobId = Guid.NewGuid();
          var charlieId = Guid.NewGuid();
          var items = new List<(Guid Id, string Name, int DeedCount)>
          {
              (aliceId, "Alice", 10),
              (bobId, "Bob", 10),
              (charlieId, "Charlie", 5)
          };
          var result = RankingHelper.Rank(items);
          
          result.Should().HaveCount(3);
          
          var alice = result.First(r => r.ChildId == aliceId);
          var bob = result.First(r => r.ChildId == bobId);
          var charlie = result.First(r => r.ChildId == charlieId);

          alice.Rank.Should().Be(1);
          bob.Rank.Should().Be(1);
          charlie.Rank.Should().Be(2); // Dense Ranking: Rank 2 instead of Rank 3
      }
  }
  ```

- [ ] **Step 2: Run tests to verify they fail**
  Run: `dotnet test --filter "FullyQualifiedName~RankingHelperTests"` in `/Volumes/PersonalProtected/GIT/TinyHeroes/backend`
  Expected: `Rank_TiedItems_ReturnsDenseRankings` fails because it returns Rank 3 instead of Rank 2.

---

### Task 2: Backend Implementation

**Files:**
- Modify: [RankingHelper.cs](file:///Volumes/PersonalProtected/GIT/TinyHeroes/backend/TinyHeroes.Application/Helpers/RankingHelper.cs)

- [ ] **Step 1: Write the minimal implementation**
  Update `RankingHelper.cs` to calculate rankings procedurally with Dense Ranking:
  ```csharp
  namespace TinyHeroes.Application.Helpers;

  public static class RankingHelper
  {
      public record RankedEntry(Guid ChildId, string ChildName, int DeedCount, int Rank);

      public static List<RankedEntry> Rank(IEnumerable<(Guid Id, string Name, int DeedCount)> items)
      {
          var sorted = items.OrderByDescending(x => x.DeedCount).ToList();
          int rank = 0;
          int? previousCount = null;
          var result = new List<RankedEntry>();
          for (int i = 0; i < sorted.Count; i++)
          {
              if (previousCount == null || sorted[i].DeedCount != previousCount.Value)
              {
                  rank++;
                  previousCount = sorted[i].DeedCount;
              }
              result.Add(new RankedEntry(sorted[i].Id, sorted[i].Name, sorted[i].DeedCount, rank));
          }
          return result;
      }
  }
  ```

- [ ] **Step 2: Run tests to verify they pass**
  Run: `dotnet test` in `/Volumes/PersonalProtected/GIT/TinyHeroes/backend`
  Expected: All 161 tests pass.

- [ ] **Step 3: Commit**
  Run:
  ```bash
  git add backend/TinyHeroes.Application/Helpers/RankingHelper.cs backend/TinyHeroes.Tests/Unit/RankingHelperTests.cs
  git commit -m "feat: implement dense ranking algorithm in backend and unit tests"
  ```

---

### Task 3: Frontend Implementation

**Files:**
- Modify: [podium.component.ts](file:///Volumes/PersonalProtected/GIT/TinyHeroes/frontend/src/app/features/podium/pages/podium.component.ts)

- [ ] **Step 1: Modify podium.component.ts**
  Replace the rank computation in the computed `allRankings` signal mapping loop (around lines 198-202):
  ```typescript
      let rank = 0, prev = -1;
      return sorted.map((c, i) => {
        if (c.weeklyCount !== prev) { rank++; prev = c.weeklyCount; }
        return { ...c, rank, eligible: this.isEligible(c.weeklyCount) };
      });
  ```

- [ ] **Step 2: Run frontend unit tests to verify**
  Run: `npm run test -- --watch=false` in `/Volumes/PersonalProtected/GIT/TinyHeroes/frontend`
  Expected: All unit tests pass.

- [ ] **Step 3: Commit**
  Run:
  ```bash
  git add frontend/src/app/features/podium/pages/podium.component.ts
  git commit -m "feat: align frontend podium ranking calculation with dense ranking"
  ```

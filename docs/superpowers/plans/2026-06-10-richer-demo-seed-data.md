# Richer Demo Seed Data Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand `DatabaseSeeder.cs` so the demo family has 4 children with varied deed counts, weekly/monthly prize minimums, historical week + month summaries, past prize claims (some marked done with comments), and family-scoped prize presets.

**Architecture:** All changes are confined to a single file — `DatabaseSeeder.cs`. We seed deterministic data using fixed GUIDs and fixed past timestamps so the data remains reproducible on every re-seed. We do not call service-layer classes from the seeder; summaries and claims are inserted directly into the database so the seeder has no runtime dependencies beyond `AppDbContext` and `UserManager<User>`.

**Tech Stack:** C# 12, EF Core (InMemory for tests, PostgreSQL for Docker), ASP.NET Identity, `System.Text.Json`

---

## File Structure

| File | Action | What changes |
|---|---|---|
| `backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs` | Modify | Add 4 children, varied deeds, min-deed limits, prize presets, historical week/month summaries, past prize claims with comments |

---

### Task 1: Expand children from 2 to 4 and add min-deed thresholds

**Files:**
- Modify: `backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs`

- [ ] **Step 1: Open the file and read its current content**

Read `backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs` to understand the current structure before editing.

- [ ] **Step 2: Replace the family block and children block**

Replace the `family` variable (after the `FamilyMember.Add` call) with a version that sets `WeeklyMinDeeds = 3` and `MonthlyMinDeeds = 12`, and replace the two `db.Children.Add` calls with four children using fixed GUIDs.

The four children (add `using TinyHeroes.Domain.Enums;` is already present):

```csharp
var familyId = Guid.Parse("00000000-0000-0000-0000-000000000002");
var family = new Family
{
    Id = familyId,
    Name = "Demo Family",
    WeekStartDay = DayOfWeek.Monday,
    WeeklyMinDeeds = 3,
    MonthlyMinDeeds = 12,
    CreatedByUserId = user.Id,
    JoinCode = "DEMO0001",
    CreatedAt = DateTime.UtcNow
};
db.Families.Add(family);

db.FamilyMembers.Add(new FamilyMember
{
    Id = Guid.NewGuid(),
    FamilyId = familyId,
    UserId = user.Id,
    Role = FamilyRole.Admin,
    JoinedAt = DateTime.UtcNow
});

var aliceId = Guid.Parse("00000000-0000-0000-0000-000000000010");
var bobId   = Guid.Parse("00000000-0000-0000-0000-000000000011");
var claraId = Guid.Parse("00000000-0000-0000-0000-000000000012");
var danId   = Guid.Parse("00000000-0000-0000-0000-000000000013");

db.Children.AddRange(
    new Child { Id = aliceId, FamilyId = familyId, Name = "Alice", Age = 5,  Gender = Gender.Girl, AvatarEmoji = "🦸‍♀️", CreatedAt = DateTime.UtcNow },
    new Child { Id = bobId,   FamilyId = familyId, Name = "Bob",   Age = 7,  Gender = Gender.Boy,  AvatarEmoji = "🦸‍♂️", CreatedAt = DateTime.UtcNow },
    new Child { Id = claraId, FamilyId = familyId, Name = "Clara", Age = 9,  Gender = Gender.Girl, AvatarEmoji = "🧝‍♀️", CreatedAt = DateTime.UtcNow },
    new Child { Id = danId,   FamilyId = familyId, Name = "Dan",   Age = 11, Gender = Gender.Boy,  AvatarEmoji = "🧙‍♂️", CreatedAt = DateTime.UtcNow }
);
```

- [ ] **Step 3: Build the backend to confirm no compile errors**

```bash
cd backend && dotnet build --no-restore 2>&1 | tail -10
```
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs
git commit -m "feat: expand demo family to 4 children with min-deed thresholds"
```

---

### Task 2: Add varied good deeds for all four children

**Files:**
- Modify: `backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs`

Context: deeds are linked to a child and an `AddedByUserId`. Use the demo parent `user.Id`. We spread deeds across several past weeks to feed the summary seeding in Task 3. We'll pin three reference Mondays (past week starts).

- [ ] **Step 1: Add deed helpers after the children block**

After the `db.Children.AddRange(...)` call, insert:

```csharp
// Reference Mondays (UTC midnight)
var week1 = new DateTime(2026, 5, 19, 0, 0, 0, DateTimeKind.Utc); // Mon 19 May
var week2 = new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc); // Mon 26 May
var week3 = new DateTime(2026, 6,  2, 0, 0, 0, DateTimeKind.Utc); // Mon  2 Jun

// Helper: add N deeds for a child in a given week, staggered by hour
void AddDeeds(Guid childId, int count, DateTime weekStart, string desc, string emoji)
{
    for (var i = 0; i < count; i++)
        db.GoodDeeds.Add(new GoodDeed
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            AddedByUserId = user.Id,
            Description = desc,
            ImageType = "emoji",
            ImageValue = emoji,
            CreatedAt = weekStart.AddHours(i + 1)
        });
}

// Week 1: Alice=5, Bob=3, Clara=7, Dan=2
AddDeeds(aliceId, 5, week1, "Helped with dishes",    "🍽️");
AddDeeds(bobId,   3, week1, "Tidied bedroom",         "🛏️");
AddDeeds(claraId, 7, week1, "Read a book",            "📚");
AddDeeds(danId,   2, week1, "Took out the trash",     "🗑️");

// Week 2: Alice=4, Bob=6, Clara=3, Dan=5
AddDeeds(aliceId, 4, week2, "Watered plants",         "🌱");
AddDeeds(bobId,   6, week2, "Helped set the table",   "🍴");
AddDeeds(claraId, 3, week2, "Practiced piano",        "🎹");
AddDeeds(danId,   5, week2, "Fed the cat",            "🐱");

// Week 3: Dan=8, Clara=6, Alice=4, Bob=4 (Alice & Bob tied at rank 3)
AddDeeds(aliceId, 4, week3, "Made the bed",           "🛏️");
AddDeeds(bobId,   4, week3, "Sorted recycling",       "♻️");
AddDeeds(claraId, 6, week3, "Helped with groceries",  "🛒");
AddDeeds(danId,   8, week3, "Cleaned the bathroom",   "🚿");
```

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build --no-restore 2>&1 | tail -10
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs
git commit -m "feat: add varied historical deeds for all four demo children"
```

---

### Task 3: Seed week summaries for the three past weeks

**Files:**
- Modify: `backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs`

Context: `WeekSummary.Rankings` is a JSON array of `{ childId, childName, deedCount, rank }` objects matching the format produced by `SummaryService`. We construct this inline; no service call needed.

- [ ] **Step 1: Add `using System.Text.Json;` at the top of the file if not already present**

Check the top of `DatabaseSeeder.cs` for `using System.Text.Json;`. If absent, add it.

- [ ] **Step 2: Insert week summary records after the deed block**

```csharp
// Week summaries — Rankings JSON mirrors SummaryService output
var weekSummary1Id = Guid.Parse("00000000-0000-0000-0000-000000000020");
var weekSummary2Id = Guid.Parse("00000000-0000-0000-0000-000000000021");
var weekSummary3Id = Guid.Parse("00000000-0000-0000-0000-000000000022");

db.WeekSummaries.AddRange(
    new WeekSummary
    {
        Id = weekSummary1Id,
        FamilyId = familyId,
        WeekStart = week1,
        WeekEnd = week1.AddDays(7),
        Rankings = JsonSerializer.Serialize(new[]
        {
            new { childId = claraId, childName = "Clara", deedCount = 7, rank = 1 },
            new { childId = aliceId, childName = "Alice", deedCount = 5, rank = 2 },
            new { childId = bobId,   childName = "Bob",   deedCount = 3, rank = 3 },
            new { childId = danId,   childName = "Dan",   deedCount = 2, rank = 4 }
        }),
        CreatedAt = week1.AddDays(7)
    },
    new WeekSummary
    {
        Id = weekSummary2Id,
        FamilyId = familyId,
        WeekStart = week2,
        WeekEnd = week2.AddDays(7),
        Rankings = JsonSerializer.Serialize(new[]
        {
            new { childId = bobId,   childName = "Bob",   deedCount = 6, rank = 1 },
            new { childId = danId,   childName = "Dan",   deedCount = 5, rank = 2 },
            new { childId = aliceId, childName = "Alice", deedCount = 4, rank = 3 },
            new { childId = claraId, childName = "Clara", deedCount = 3, rank = 4 }
        }),
        CreatedAt = week2.AddDays(7)
    },
    new WeekSummary
    {
        Id = weekSummary3Id,
        FamilyId = familyId,
        WeekStart = week3,
        WeekEnd = week3.AddDays(7),
        Rankings = JsonSerializer.Serialize(new[]
        {
            new { childId = danId,   childName = "Dan",   deedCount = 8, rank = 1 },
            new { childId = claraId, childName = "Clara", deedCount = 6, rank = 2 },
            new { childId = aliceId, childName = "Alice", deedCount = 4, rank = 3 },
            new { childId = bobId,   childName = "Bob",   deedCount = 4, rank = 3 }  // tied at rank 3 — no rank 4
        }),
        CreatedAt = week3.AddDays(7)
    }
);
```

- [ ] **Step 3: Build**

```bash
cd backend && dotnet build --no-restore 2>&1 | tail -10
```
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs
git commit -m "feat: seed three historical week summaries for demo family"
```

---

### Task 4: Seed a month summary for May 2026

**Files:**
- Modify: `backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs`

Context: May 2026 spans week1 and week2 (19 May–1 Jun), week3 starts 2 Jun (not May). Aggregate deed counts for May: Alice=5+4=9, Bob=3+6=9, Clara=7+3=10, Dan=2+5=7. TotalDeeds=35. Clara champion with 10.

- [ ] **Step 1: Insert month summary after the week summaries block**

```csharp
var monthSummary1Id = Guid.Parse("00000000-0000-0000-0000-000000000030");

db.MonthSummaries.Add(new MonthSummary
{
    Id = monthSummary1Id,
    FamilyId = familyId,
    Year = 2026,
    Month = 5,
    ChampionChildId = claraId,
    ChampionName = "Clara",
    TotalDeeds = 35,
    Rankings = JsonSerializer.Serialize(new[]
    {
        new { childId = claraId, childName = "Clara", deedCount = 10, rank = 1 },
        new { childId = aliceId, childName = "Alice", deedCount = 9,  rank = 2 },
        new { childId = bobId,   childName = "Bob",   deedCount = 9,  rank = 2 },
        new { childId = danId,   childName = "Dan",   deedCount = 7,  rank = 4 }
    }),
    CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
});
```

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build --no-restore 2>&1 | tail -10
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs
git commit -m "feat: seed May 2026 month summary for demo family"
```

---

### Task 5: Seed prize assignments (weekly rank 1/2/3 + monthly)

**Files:**
- Modify: `backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs`

Context: `PrizeAssignment` is a configuration table — it defines what prizes exist for each rank/scope for the family. These are not per-week; they describe the standing prize structure.

- [ ] **Step 1: Insert prize assignments after the month summary block**

```csharp
db.PrizeAssignments.AddRange(
    new PrizeAssignment { Id = Guid.NewGuid(), FamilyId = familyId, Scope = "weekly", Rank = 1, Emoji = "🥇", Label = "Gold Medal",   UpdatedAt = DateTime.UtcNow },
    new PrizeAssignment { Id = Guid.NewGuid(), FamilyId = familyId, Scope = "weekly", Rank = 2, Emoji = "🥈", Label = "Silver Medal", UpdatedAt = DateTime.UtcNow },
    new PrizeAssignment { Id = Guid.NewGuid(), FamilyId = familyId, Scope = "weekly", Rank = 3, Emoji = "🥉", Label = "Bronze Medal", UpdatedAt = DateTime.UtcNow },
    new PrizeAssignment { Id = Guid.NewGuid(), FamilyId = familyId, Scope = "monthly", Rank = null, Emoji = "🏆", Label = "Monthly Champion Trophy", UpdatedAt = DateTime.UtcNow }
);
```

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build --no-restore 2>&1 | tail -10
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs
git commit -m "feat: seed demo prize assignments (weekly ranks + monthly)"
```

---

### Task 6: Seed past prize claims — some used, some with comments

**Files:**
- Modify: `backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs`

Context: `PrizeClaim` is the record of a prize being awarded. We seed claims for week1, week2, and the May monthly. Some claims are marked `IsUsed = true`, some have `PrizeComment` children.

- [ ] **Step 1: Insert prize claims and their comments after the prize assignment block**

```csharp
// --- Week 1 claims ---
var claimW1R1Id = Guid.Parse("00000000-0000-0000-0000-000000000040");
var claimW1R2Id = Guid.Parse("00000000-0000-0000-0000-000000000041");
var claimW1R3Id = Guid.Parse("00000000-0000-0000-0000-000000000042");

db.PrizeClaims.AddRange(
    new PrizeClaim
    {
        Id = claimW1R1Id, FamilyId = familyId, Scope = "weekly",
        WeekSummaryId = weekSummary1Id, Rank = 1,
        ChildId = claraId, ChildName = "Clara",
        PrizeEmoji = "🥇", PrizeLabel = "Gold Medal",
        IsUsed = true, UsedAt = week1.AddDays(8),
        CreatedAt = week1.AddDays(7)
    },
    new PrizeClaim
    {
        Id = claimW1R2Id, FamilyId = familyId, Scope = "weekly",
        WeekSummaryId = weekSummary1Id, Rank = 2,
        ChildId = aliceId, ChildName = "Alice",
        PrizeEmoji = "🥈", PrizeLabel = "Silver Medal",
        IsUsed = true, UsedAt = week1.AddDays(9),
        CreatedAt = week1.AddDays(7)
    },
    new PrizeClaim
    {
        Id = claimW1R3Id, FamilyId = familyId, Scope = "weekly",
        WeekSummaryId = weekSummary1Id, Rank = 3,
        ChildId = bobId, ChildName = "Bob",
        PrizeEmoji = "🥉", PrizeLabel = "Bronze Medal",
        IsUsed = false,
        CreatedAt = week1.AddDays(7)
    }
);

// Comments on week-1 claims
db.PrizeComments.AddRange(
    new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimW1R1Id, Text = "Well done Clara, you earned it! 🎉", CreatedAt = week1.AddDays(7).AddHours(2) },
    new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimW1R1Id, Text = "Can I choose an ice cream? 🍦",       CreatedAt = week1.AddDays(7).AddHours(4) },
    new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimW1R2Id, Text = "Great job Alice! 🌟",                  CreatedAt = week1.AddDays(8).AddHours(1) }
);

// --- Week 2 claims ---
var claimW2R1Id = Guid.Parse("00000000-0000-0000-0000-000000000043");

db.PrizeClaims.Add(new PrizeClaim
{
    Id = claimW2R1Id, FamilyId = familyId, Scope = "weekly",
    WeekSummaryId = weekSummary2Id, Rank = 1,
    ChildId = bobId, ChildName = "Bob",
    PrizeEmoji = "🥇", PrizeLabel = "Gold Medal",
    IsUsed = false,
    CreatedAt = week2.AddDays(7)
});

db.PrizeComments.Add(
    new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimW2R1Id, Text = "Amazing week Bob! 💪", CreatedAt = week2.AddDays(7).AddHours(1) }
);

// --- May 2026 monthly claim ---
var claimMay1Id = Guid.Parse("00000000-0000-0000-0000-000000000050");

db.PrizeClaims.Add(new PrizeClaim
{
    Id = claimMay1Id, FamilyId = familyId, Scope = "monthly",
    MonthSummaryId = monthSummary1Id, Rank = null,
    ChildId = claraId, ChildName = "Clara",
    PrizeEmoji = "🏆", PrizeLabel = "Monthly Champion Trophy",
    IsUsed = true, UsedAt = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc),
    CreatedAt = new DateTime(2026, 6, 1, 0, 30, 0, DateTimeKind.Utc)
});

db.PrizeComments.AddRange(
    new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimMay1Id, Text = "Clara is the champion of May! 🏆",             CreatedAt = new DateTime(2026, 6, 1, 1, 0, 0, DateTimeKind.Utc) },
    new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimMay1Id, Text = "I want to go to the theme park as my prize!", CreatedAt = new DateTime(2026, 6, 1, 2, 0, 0, DateTimeKind.Utc) },
    new PrizeComment { Id = Guid.NewGuid(), PrizeClaimId = claimMay1Id, Text = "Done! We went on the 2nd 🎢",                  CreatedAt = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc) }
);
```

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build --no-restore 2>&1 | tail -10
```
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs
git commit -m "feat: seed past prize claims with used status and comments"
```

---

### Task 7: Seed family-scoped prize presets

**Files:**
- Modify: `backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs`

Context: `PrizePreset` rows with a non-null `FamilyId` appear as custom presets in the UI prize picker. The global (null `FamilyId`) presets come from other seed logic; here we add family-specific extras.

- [ ] **Step 1: Insert family prize presets at the end of the seeder, before `SaveChangesAsync`**

```csharp
db.PrizePresets.AddRange(
    new PrizePreset { Id = Guid.NewGuid(), FamilyId = familyId, Label = "Extra screen time", Emoji = "📱", Enabled = true, CreatedAt = DateTime.UtcNow },
    new PrizePreset { Id = Guid.NewGuid(), FamilyId = familyId, Label = "Choose dinner",      Emoji = "🍕", Enabled = true, CreatedAt = DateTime.UtcNow },
    new PrizePreset { Id = Guid.NewGuid(), FamilyId = familyId, Label = "Bedtime 30 min late",Emoji = "🌙", Enabled = true, CreatedAt = DateTime.UtcNow },
    new PrizePreset { Id = Guid.NewGuid(), FamilyId = familyId, Label = "Cinema trip",        Emoji = "🎬", Enabled = false, CreatedAt = DateTime.UtcNow }
);
```

(One preset disabled to demonstrate the disabled state in the UI.)

- [ ] **Step 2: Build the full solution**

```bash
cd backend && dotnet build --no-restore 2>&1 | tail -10
```
Expected: `Build succeeded.`

- [ ] **Step 3: Run all tests to ensure nothing is broken**

```bash
cd backend && dotnet test 2>&1 | tail -20
```
Expected: all tests pass (seeder is only invoked at runtime, not in integration tests which use their own in-memory factories).

- [ ] **Step 4: Commit**

```bash
git add backend/TinyHeroes.Infrastructure/Data/DatabaseSeeder.cs
git commit -m "feat: add family-scoped prize presets to demo seed data"
```

---

### Task 8: Smoke-test with Docker

**Files:** No code changes — verification only.

- [ ] **Step 1: Rebuild and restart Docker**

```bash
cd /path/to/repo && docker compose up -d --build
```

- [ ] **Step 2: Log in as the demo user**

Open `http://localhost:4200`, log in with `testuser@demo.com` / `Password1!`.

- [ ] **Step 3: Verify children**

Navigate to the family page. Confirm 4 children appear: Alice (5), Bob (7), Clara (9), Dan (11).

- [ ] **Step 4: Verify history page**

Open the Prize History page. Confirm:
- 3 week summaries are visible (weeks of 19 May, 26 May, 2 Jun)
- The May 2026 monthly summary is visible with Clara as champion
- Claims for week-1 rank 1 and 2 are marked used; rank 3 is not
- Comments appear on the week-1 gold and silver claims and on the May monthly claim

- [ ] **Step 5: Verify prize presets in family settings**

Open family settings → Prize Presets. Confirm 4 family presets appear, one shown as disabled.

- [ ] **Step 6: Verify weekly min-deed threshold is reflected**

In family settings, confirm `WeeklyMinDeeds = 3` and `MonthlyMinDeeds = 12` are set.

---

## Self-Review

**Spec coverage check:**
- ✅ 4 children — Task 1
- ✅ Different deed amounts per child per week — Task 2 (week1: 7/5/3/2, week2: 6/5/4/3, week3: 8/6/4/4)
- ✅ Deed equality for 2 children in one week — Task 2 (week3: Alice=4, Bob=4 tied at rank 3)
- ✅ Weekly and monthly limits — Task 1 (WeeklyMinDeeds / MonthlyMinDeeds)
- ✅ Prizes in the past (won prizes) — Tasks 3–6
- ✅ Comments on prizes — Task 6 (PrizeComments)
- ✅ Some prizes already marked done — Task 6 (IsUsed = true)
- ✅ Etc. / prize presets — Task 7

**Type consistency check:**
- `weekSummary1Id`, `weekSummary2Id`, `weekSummary3Id` defined in Task 3, referenced in Task 6 ✅
- `monthSummary1Id` defined in Task 4, referenced in Task 6 ✅
- `aliceId`, `bobId`, `claraId`, `danId` defined in Task 1, used in Tasks 2–6 ✅
- `familyId` defined in Task 1, used in all subsequent tasks ✅
- `user.Id` available from the user registration block at the top ✅

**Placeholder scan:** No TBDs, no "fill in details" — all code blocks are complete.

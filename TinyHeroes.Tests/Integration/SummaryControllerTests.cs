using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TinyHeroes.Application.DTOs.Child;
using TinyHeroes.Application.DTOs.Deed;
using TinyHeroes.Application.DTOs.Summary;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Infrastructure.Data;
using TinyHeroes.Tests.Integration.Helpers;

namespace TinyHeroes.Tests.Integration;

public class SummaryControllerTests(TestWebApplicationFactory<Program> factory)
    : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetWeeks_WithNoDeeds_ReturnsEmptyList()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);

        var response = await client.GetAsync("/api/summaries/weeks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summaries = await response.Content.ReadFromJsonAsync<List<WeekSummaryResponse>>();
        summaries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWeeks_WithPastDeeds_GeneratesSummary()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Alice", 7, Gender.Girl, "🦄"));
        var child = await childResp.Content.ReadFromJsonAsync<ChildResponse>();

        // Insert a deed dated 14 days ago directly via DB
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deed = new GoodDeed
        {
            Id = Guid.NewGuid(),
            ChildId = child!.Id,
            AddedByUserId = Guid.NewGuid(),
            Description = "Past deed",
            ImageType = "library",
            ImageValue = "⭐",
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };
        db.GoodDeeds.Add(deed);
        await db.SaveChangesAsync();

        var response = await client.GetAsync("/api/summaries/weeks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summaries = await response.Content.ReadFromJsonAsync<List<WeekSummaryResponse>>();
        summaries.Should().NotBeEmpty();
        summaries!.Should().Contain(s => s.Rankings.Any(r => r.ChildId == child.Id && r.DeedCount > 0));
    }

    [Fact]
    public async Task GetMonths_WithPastDeeds_GeneratesSummary()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Bob", 5, Gender.Boy, "🦁"));
        var child = await childResp.Content.ReadFromJsonAsync<ChildResponse>();

        // Insert a deed dated 90 days ago directly via DB (safely in a past completed month)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deed = new GoodDeed
        {
            Id = Guid.NewGuid(),
            ChildId = child!.Id,
            AddedByUserId = Guid.NewGuid(),
            Description = "Old deed",
            ImageType = "library",
            ImageValue = "🌟",
            CreatedAt = DateTime.UtcNow.AddDays(-90)
        };
        db.GoodDeeds.Add(deed);
        await db.SaveChangesAsync();

        var response = await client.GetAsync("/api/summaries/months");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summaries = await response.Content.ReadFromJsonAsync<List<MonthSummaryResponse>>();
        summaries.Should().NotBeEmpty();
        summaries!.Should().Contain(s => s.ChampionChildId == child.Id);
    }

    [Fact]
    public async Task GetCurrentMonth_ReturnsLiveStats()
    {
        var client = await TestAuthHelper.RegisterWithFamily(factory);
        var childResp = await client.PostAsJsonAsync("/api/children", new CreateChildRequest("Charlie", 8, Gender.Boy, "🐻"));
        var child = await childResp.Content.ReadFromJsonAsync<ChildResponse>();

        // Add a deed via API (CreatedAt = now = current month)
        await client.PostAsJsonAsync("/api/deeds", new CreateDeedRequest(child!.Id, "Good deed today", "⭐"));

        var response = await client.GetAsync("/api/summaries/current-month");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var currentMonth = await response.Content.ReadFromJsonAsync<CurrentMonthResponse>();
        currentMonth.Should().NotBeNull();
        currentMonth!.Year.Should().Be(DateTime.UtcNow.Year);
        currentMonth.Month.Should().Be(DateTime.UtcNow.Month);
        currentMonth.Rankings.Should().Contain(r => r.ChildId == child.Id && r.DeedCount == 1);
    }

    [Fact]
    public async Task GetWeeks_WithoutFamily_Returns400()
    {
        var client = await TestAuthHelper.RegisterOnly(factory);

        var response = await client.GetAsync("/api/summaries/weeks");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

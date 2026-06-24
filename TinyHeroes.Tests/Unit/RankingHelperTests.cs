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

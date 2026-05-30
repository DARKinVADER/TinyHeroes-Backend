namespace TinyHeroes.Application.Helpers;

public static class RankingHelper
{
    public record RankedEntry(Guid ChildId, string ChildName, int DeedCount, int Rank);

    public static List<RankedEntry> Rank(IEnumerable<(Guid Id, string Name, int DeedCount)> items)
    {
        var sorted = items.OrderByDescending(x => x.DeedCount).ToList();
        int rank = 0;
        int previousCount = -1;
        var result = new List<RankedEntry>();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].DeedCount != previousCount)
            {
                rank = i + 1;
                previousCount = sorted[i].DeedCount;
            }
            result.Add(new RankedEntry(sorted[i].Id, sorted[i].Name, sorted[i].DeedCount, rank));
        }
        return result;
    }
}

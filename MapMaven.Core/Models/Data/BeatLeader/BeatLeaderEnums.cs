namespace MapMaven.Core.Models.Data.BeatLeader
{
    public enum SortBy
    {
        None,
        Timestamp,
        Name,
        Stars,
        PassRating,
        AccRating,
        TechRating,
        ScoreTime,
        PlayCount,
        Voting,
        VoteCount,
        VoteRatio,
        Duration,
    }

    public enum Order
    {
        Desc,
        Asc,
    }

    public enum LeaderboardType
    {
        All,
        Ranked,
        Ranking,
        Nominated,
        Qualified,
        Staff,
        Reweighting,
        Reweighted,
        Unranked,
    }

    [Flags]
    public enum LeaderboardContexts
    {
        None = 0,
        General = 1 << 1,
        NoMods = 1 << 2,
        NoPause = 1 << 3,
        Golf = 1 << 4
    }
}

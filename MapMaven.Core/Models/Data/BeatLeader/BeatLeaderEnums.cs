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
}

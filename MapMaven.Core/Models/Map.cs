﻿using BeatSaverSharp.Models;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.Leaderboards;
using MapMaven.Core.Models.Data.RankedMaps;

namespace MapMaven.Models
{
    public class Map
    {
        public string Id { get; set; }
        public string Hash { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string SongAuthorName { get; set; }
        public string MapAuthorName { get; set; }
        public DateTime? AddedDateTime { get; set; }
        public TimeSpan SongDuration { get; set; }
        public TimeSpan PreviewStartTime { get; set; }
        public TimeSpan PreviewDuration { get; set; }
        public TimeSpan PreviewEndTime => PreviewStartTime + PreviewDuration;
        public string CoverImageUrl { get; set; }
        public bool Hidden { get; set; }
        public IEnumerable<string> Tags { get; set; } = Enumerable.Empty<string>();
        public int? Bpm { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public bool Played => HighestPlayerScore != null;
        public bool Ranked => RankedMap != null;

        public PlayerScore? HighestPlayerScore { get; set; }
        public IEnumerable<PlayerScore> AllPlayerScores { get; set; } = Enumerable.Empty<PlayerScore>();
        public RankedMapInfoItem RankedMap { get; private set; }
        public IEnumerable<MapDifficulty> Difficulties { get; set; } = Enumerable.Empty<MapDifficulty>();
        public RankedMapDifficultyInfo? Difficulty { get; set; }
        public IEnumerable<ScoreEstimate> ScoreEstimates { get; set; } = Enumerable.Empty<ScoreEstimate>();
        public ScoreEstimate? ScoreEstimate => ScoreEstimates.FirstOrDefault();

        public Map() { }

        public Map(Beatmap beatmap)
        {
            Id = beatmap.ID;
            Hash = beatmap.LatestVersion.Hash;
            Name = beatmap.Name;
            SongAuthorName = beatmap.Metadata.SongAuthorName;
            MapAuthorName = beatmap.Metadata.LevelAuthorName;
            SongDuration = TimeSpan.FromSeconds(beatmap.Metadata.Duration);
            CoverImageUrl = beatmap.LatestVersion.CoverURL;

            SetMapDetails(beatmap);
        }

        public void SetRankedMapDetails(RankedMapInfoItem rankedMap)
        {
            if (rankedMap == null)
                return;

            RankedMap = rankedMap;
            Difficulties = rankedMap.Difficulties.Select(d => new MapDifficulty(d));
            Tags = rankedMap.Tags ?? Enumerable.Empty<string>();
        }

        public void SetMapDetails(Beatmap beatmap)
        {
            Tags = beatmap.Tags ?? Enumerable.Empty<string>();
            ReleaseDate = beatmap.Uploaded;
            Bpm = (int)beatmap.Metadata.BPM;
            Description = beatmap.Description;

            if (Difficulties.Any() || beatmap.LatestVersion == null)
                return;

            Difficulties = beatmap.LatestVersion.Difficulties.Select(d => new Core.Models.MapDifficulty(d));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (!(obj is Map other))
                return false;

            return Hash == other.Hash && (Difficulty is null || Difficulty == other.Difficulty);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hash, Difficulty);
        }
    }
}

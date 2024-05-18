using ApexCharts;
using MapMaven.Core.Models;
using MapMaven.Core.Models.AdvancedSearch;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Utilities.BeatSaver;
using MapMaven.Models;
using MapMaven.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Utilities;
using System.Globalization;
using System.Reactive.Linq;
using static Microsoft.Maui.ApplicationModel.Permissions;
using Map = MapMaven.Models.Map;

namespace MapMaven.Pages
{
    public partial class Dashboard
    {
        [Inject]
        ILeaderboardService LeaderboardService { get; set; }

        [Inject]
        IMapService MapService { get; set; }

        PlayerProfile Player { get; set; }

        ApexChart<RankHistoryRecord> RankHistoryChart;

        ApexChartOptions<RankHistoryRecord> RankHistoryChartOptions = new()
        {
            Annotations = new(),
            Chart = new()
            {
                Background = "transparent",
                ForeColor = MapMavenTheme.Theme.Palette.TextPrimary.SetAlpha(0.3).ToString(MudColorOutputFormats.RGBA),
                Toolbar = new()
                {
                    Show = false
                },
                Zoom = new()
                {
                    Enabled = false
                },
                Animations = new()
                {
                    Enabled = false
                }
            },
            Yaxis = [
                new() { Reversed = true }
            ],
            Tooltip = new()
            {
                Custom = "dashboard.rankHistoryTooltip"
            },
            Grid = new()
            {
                BorderColor = MapMavenTheme.Theme.Palette.TextPrimary.SetAlpha(0.3).ToString(MudColorOutputFormats.RGBA),
            },
            Theme = new()
            {
                Mode = Mode.Dark,
            }
        };

        double? RankChange { get; set; }
        double? CountryRankChange { get; set; }
        double? PpChange { get; set; }

        double? TopPp { get; set; }
        double? AverageStarDifficulty { get; set; }
        double? AverageRankedAccuracy { get; set; }

        double? RecentAverageStarDifficulty { get; set; }
        string? RecentBestScoredMapTag { get; set; }


        IEnumerable<string> BestScoredMapTags { get; set; } = [];

        IEnumerable<Map> RecentHighPpGainMaps { get; set; } = [];
        IEnumerable<Map> RecommendedMaps { get; set; } = [];

        protected override void OnInitialized()
        {
            SubscribeAndBind(LeaderboardService.PlayerProfile, player =>
            {
                Player = player;

                var earliestRankHistoryRecord = player.RankHistory.FirstOrDefault();
                var latestRankHistoryRecord = player.RankHistory.LastOrDefault();

                if (earliestRankHistoryRecord is not null && latestRankHistoryRecord is not null)
                    RankChange = earliestRankHistoryRecord.Rank - latestRankHistoryRecord.Rank;

                if (earliestRankHistoryRecord is not null && latestRankHistoryRecord is not null)
                    CountryRankChange = earliestRankHistoryRecord.CountryRank - latestRankHistoryRecord.CountryRank;

                if (earliestRankHistoryRecord is not null && latestRankHistoryRecord is not null)
                    PpChange = latestRankHistoryRecord.Pp - earliestRankHistoryRecord.Pp;

                InvokeAsync(async () =>
                {
                    if (RankHistoryChart is null)
                        return;

                    await RankHistoryChart.UpdateSeriesAsync();
                    StateHasChanged();
                });
            });

            SubscribeAndBind(LeaderboardService.PlayerScores, scores =>
            {
                if (scores is null || !scores.Any())
                    return;

                var rankedScores = scores.Where(s => s.Leaderboard.Stars > 0);

                TopPp = rankedScores.Max(s => s.Score.Pp);

                AverageStarDifficulty = rankedScores
                    .OrderByDescending(s => s.Score.Pp)
                    .Take(30)
                    .Average(s => s.Leaderboard.Stars);

                AverageRankedAccuracy = rankedScores
                    .OrderByDescending(s => s.Score.Pp)
                .Average(s => s.Score.Accuracy);
            });

            var mapsAndHistory = Observable.CombineLatest(MapService.RankedMaps, LeaderboardService.PlayerProfile, (rankedMaps, playerProfile) => (rankedMaps, playerProfile));

            SubscribeAndBind(mapsAndHistory, x =>
            {
                var playedRankedMaps = x.rankedMaps.Where(m => m.Played);

                BestScoredMapTags = playedRankedMaps
                    .OrderByDescending(m => m.HighestPlayerScore?.Score.Pp ?? 0)
                    .SelectMany(m => m.Tags)
                    .GroupBy(t => t)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key);

                var recentPlayedRankedMaps = playedRankedMaps
                    .Where(m => m.HighestPlayerScore is not null && m.HighestPlayerScore.Score.TimeSet > DateTimeOffset.Now.AddDays(-50))
                    .OrderBy(m => m.HighestPlayerScore.Score.TimeSet);

                var rankIncreaseHistoryRecords = x.playerProfile.RankHistory
                    .Zip(x.playerProfile.RankHistory.Skip(1), (previousHistoryRecord, historyRecord) => (previousHistoryRecord, historyRecord))
                    .Where(x => x.historyRecord.Rank < x.previousHistoryRecord.Rank)
                    .Select(x => x.historyRecord);

                RecentHighPpGainMaps = rankIncreaseHistoryRecords
                    .SelectMany(historyRecord => recentPlayedRankedMaps
                        .Where(map =>
                        {
                            var scoreDate = DateOnly.FromDateTime(map.HighestPlayerScore.Score.TimeSet.DateTime);

                            return scoreDate >= historyRecord.Date.AddDays(-1) && scoreDate <= historyRecord.Date.AddDays(1);
                        })
                    ).Distinct()
                    .OrderByDescending(m => m.HighestPlayerScore.Score.WeightedPp);

                var recentPlayedMapsWithDifficulty = recentPlayedRankedMaps
                    .Where(m => m.Difficulty is not null);

                if (recentPlayedMapsWithDifficulty.Any())
                    RecentAverageStarDifficulty = recentPlayedMapsWithDifficulty.Average(m => m.Difficulty.Stars);

                RecentBestScoredMapTag = recentPlayedRankedMaps
                    .OrderByDescending(m => m.HighestPlayerScore?.Score.Pp ?? 0)
                    .SelectMany(m => m.Tags)
                    .GroupBy(t => t)
                    .Where(g => MapTag.IsDisciplineTag(g.Key))
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key;

                var recommendedMapsDynamicPlaylistConfiguration = new DynamicPlaylistConfiguration
                {
                    FilterOperations = new()
                    {
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Stars),
                            Operator = FilterOperator.GreaterThanOrEqual,
                            Value = RecentAverageStarDifficulty?.ToString(CultureInfo.InvariantCulture) ?? "0"
                        },
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Tags),
                            Operator = FilterOperator.Contains,
                            Value = RecentBestScoredMapTag
                        },
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Hidden),
                            Operator = FilterOperator.Equals,
                            Value = "false"
                        },
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Played),
                            Operator = FilterOperator.Equals,
                            Value = "false"
                        },
                        new FilterOperation
                        {
                            Field = $"{nameof(AdvancedSearchMap.ScoreEstimate)}.{nameof(AdvancedSearchMap.ScoreEstimate.PPIncrease)}",
                            Operator = FilterOperator.GreaterThanOrEqual,
                            Value = "0"
                        },
                        new FilterOperation
                        {
                            Field = $"{nameof(AdvancedSearchMap.ScoreEstimate)}.{nameof(AdvancedSearchMap.ScoreEstimate.Accuracy)}",
                            Operator = FilterOperator.GreaterThanOrEqual,
                            Value = 80.ToString()
                        }
                    },
                    SortOperations = new()
                    {
                        new SortOperation
                        {
                            Field = $"{nameof(AdvancedSearchMap.ScoreEstimate)}.{nameof(AdvancedSearchMap.ScoreEstimate.PPIncrease)}",
                            Direction = SortDirection.Descending
                        }
                    }
                };

                RecommendedMaps = x.rankedMaps;

                foreach (var filterOperation in recommendedMapsDynamicPlaylistConfiguration.FilterOperations)
                {
                    RecommendedMaps = RecommendedMaps.Where(map => MapSearchService.FilterOperationMatches(new AdvancedSearchMap(map), filterOperation));
                }

                RecommendedMaps = MapSearchService.SortMaps(RecommendedMaps, recommendedMapsDynamicPlaylistConfiguration.SortOperations, x => new AdvancedSearchMap(x));

                InvokeAsync(async () =>
                {
                    if (RankHistoryChart is null)
                        return;

                    await RankHistoryChart.UpdateOptionsAsync(true, false, false);
                    StateHasChanged();
                });
            });
        }

        void SetCustomData(DataPoint<RankHistoryRecord> dataPoint)
        {
            dataPoint.Extra = RecentHighPpGainMaps
                .Where(map =>
                {
                    var scoreDate = DateOnly.FromDateTime(map.HighestPlayerScore.Score.TimeSet.DateTime);

                    return scoreDate == (DateOnly)dataPoint.X;
                })
                .Select(map => new
                {
                    name = map.Name,
                    coverImageUrl = map.CoverImageUrl,
                    difficulty = DifficultyDisplayUtils.GetShortName(map.Difficulty.Difficulty),
                    difficultyColor = DifficultyDisplayUtils.GetColor(map.Difficulty.Difficulty),
                    pp = map.HighestPlayerScore.Score.Pp.ToString("0.##"),
                });
        }
    }
}
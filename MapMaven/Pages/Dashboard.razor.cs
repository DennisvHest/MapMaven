using ApexCharts;
using MapMaven.Core.Models;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Utilities;
using System.Reactive.Linq;
using Map = MapMaven.Models.Map;

namespace MapMaven.Pages
{
    public partial class Dashboard
    {
        [Inject]
        ILeaderboardService LeaderboardService { get; set; }

        [Inject]
        IMapService MapService { get; set; }

        [Inject]
        IJSRuntime Js { get; set; }

        IJSObjectReference? dashboardJsModule;

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
                Custom = "rankHistoryTooltip"
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

        double? TopPp { get; set; }
        double? AverageStarDifficulty { get; set; }
        double? AverageRankedAccuracy { get; set; }

        IEnumerable<string> BestScoredMapTags { get; set; } = Enumerable.Empty<string>();

        IEnumerable<Map> RecentHighPpGainMaps { get; set; } = Enumerable.Empty<Map>();

        protected override void OnInitialized()
        {
            SubscribeAndBind(LeaderboardService.PlayerProfile, player =>
            {
                Player = player;

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
                });
        }
    }
}
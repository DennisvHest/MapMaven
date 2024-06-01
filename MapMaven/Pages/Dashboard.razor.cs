using ApexCharts;
using FastDeepCloner;
using MapMaven.Components.Playlists;
using MapMaven.Components.Shared;
using MapMaven.Core.Models;
using MapMaven.Core.Models.AdvancedSearch;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Utilities.BeatSaver;
using MapMaven.Extensions;
using MapMaven.Models;
using MapMaven.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
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

        [Inject]
        IPlaylistService PlaylistService { get; set; }

        [Inject]
        IDialogService DialogService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

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

        DynamicPlaylistConfiguration? RecommendedMapsDynamicPlaylistConfiguration { get; set; }

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

                RecommendedMapsDynamicPlaylistConfiguration = new DynamicPlaylistConfiguration
                {
                    MapCount = 20,
                    MapPool = MapPool.Improvement,
                    FilterOperations = new()
                    {
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Stars),
                            Operator = Core.Models.DynamicPlaylists.FilterOperator.GreaterThanOrEqual,
                            Value = RecentAverageStarDifficulty?.ToString(CultureInfo.InvariantCulture) ?? "0"
                        },
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Tags),
                            Operator = Core.Models.DynamicPlaylists.FilterOperator.Contains,
                            Value = RecentBestScoredMapTag
                        },
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Hidden),
                            Operator = Core.Models.DynamicPlaylists.FilterOperator.Equals,
                            Value = "false"
                        },
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Played),
                            Operator = Core.Models.DynamicPlaylists.FilterOperator.Equals,
                            Value = "false"
                        },
                        new FilterOperation
                        {
                            Field = $"{nameof(AdvancedSearchMap.ScoreEstimate)}.{nameof(AdvancedSearchMap.ScoreEstimate.PPIncrease)}",
                            Operator = Core.Models.DynamicPlaylists.FilterOperator.GreaterThanOrEqual,
                            Value = "0"
                        },
                        new FilterOperation
                        {
                            Field = $"{nameof(AdvancedSearchMap.ScoreEstimate)}.{nameof(AdvancedSearchMap.ScoreEstimate.Accuracy)}",
                            Operator = Core.Models.DynamicPlaylists.FilterOperator.GreaterThanOrEqual,
                            Value = 80.ToString()
                        }
                    },
                    SortOperations = new()
                    {
                        new SortOperation
                        {
                            Field = $"{nameof(AdvancedSearchMap.ScoreEstimate)}.{nameof(AdvancedSearchMap.ScoreEstimate.PPIncrease)}",
                            Direction = Core.Models.DynamicPlaylists.SortDirection.Descending
                        }
                    }
                };

                RecommendedMaps = x.rankedMaps;

                foreach (var filterOperation in RecommendedMapsDynamicPlaylistConfiguration.FilterOperations)
                {
                    RecommendedMaps = RecommendedMaps.Where(map => MapSearchService.FilterOperationMatches(new AdvancedSearchMap(map), filterOperation));
                }

                RecommendedMaps = MapSearchService.SortMaps(RecommendedMaps, RecommendedMapsDynamicPlaylistConfiguration.SortOperations, x => new AdvancedSearchMap(x));

                RecommendedMaps = RecommendedMaps.Take(20);

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

        async Task AddRecommendedMapsToPlaylist()
        {
            var dialog = await DialogService.ShowAsync<PlaylistSelector>($"Add recommended maps to playlist", new DialogParameters
            {
                { nameof(PlaylistSelector.SaveNewPlaylistOnSubmit), false }
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true,
                CloseButton = true
            });

            var result = await dialog.Result;

            if (result.Canceled)
                return;

            var snackbar = Snackbar.AddMapDownloadProgressSnackbar();

            var playlist = result.Data switch
            {
                Playlist resultPlaylist => await PlaylistService.AddPlaylistAndDownloadMaps(resultPlaylist, RecommendedMaps, progress: snackbar.Progress, cancellationToken: snackbar.CancellationToken),
                EditPlaylistModel resultEditPlaylist => await PlaylistService.AddPlaylistAndDownloadMaps(resultEditPlaylist, RecommendedMaps, progress: snackbar.Progress, cancellationToken: snackbar.CancellationToken),
                _ => throw new InvalidOperationException("Result data is not a playlist")
            };

            Snackbar.Remove(snackbar.Snackbar);

            if (!snackbar.CancellationToken.IsCancellationRequested)
            {
                Snackbar.Add($"Created playlist: {playlist.Title}", Severity.Normal, config =>
                {
                    config.Icon = Icons.Material.Filled.Check;

                    config.Action = "Open";
                    config.ActionColor = MudBlazor.Color.Primary;
                    config.Onclick = snackbar =>
                    {
                        PlaylistService.SetSelectedPlaylist(playlist);
                        NavigationManager.NavigateTo("/");
                        return Task.CompletedTask;
                    };
                });
            }
            else
            {
                Snackbar.Add($"Cancelled creating playlist.", Severity.Normal, config => config.Icon = Icons.Material.Filled.Cancel);
            }
        }

        async Task AddRecommendedMapsDynamicPlaylist()
        {
            var parameters = new DialogParameters
            {
                { "SelectedPlaylist", new EditDynamicPlaylistModel { DynamicPlaylistConfiguration = DeepCloner.Clone(RecommendedMapsDynamicPlaylistConfiguration)} }
            };

            DialogService.Show<EditDynamicPlaylistDialog>("Add playlist", parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }
    }
}
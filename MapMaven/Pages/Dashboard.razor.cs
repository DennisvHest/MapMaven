using ApexCharts;
using FastDeepCloner;
using MapMaven.Components.Playlists;
using MapMaven.Core.Models;
using MapMaven.Core.Models.AdvancedSearch;
using MapMaven.Core.Models.LivePlaylists;
using MapMaven.Core.Models.LivePlaylists.MapInfo;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Services.Leaderboards.ScoreEstimation;
using MapMaven.Core.Utilities.BeatSaver;
using MapMaven.Extensions;
using MapMaven.Models;
using MapMaven.Services;
using MapMaven.Utility;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Map = MapMaven.Models.Map;

namespace MapMaven.Pages
{
    public partial class Dashboard
    {
        [Inject]
        ILeaderboardService LeaderboardService { get; set; }

        [Inject]
        IBeatSaberDataService BeatSaberDataService { get; set; }

        [Inject]
        IMapService MapService { get; set; }

        [Inject]
        IPlaylistService PlaylistService { get; set; }

        [Inject]
        IEnumerable<IScoreEstimationService> ScoreEstimationServices { get; set; }

        [Inject]
        IDialogService DialogService { get; set; }

        [Inject]
        ISnackbar Snackbar { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        bool LoadingDashboardData { get; set; } = false;

        PlayerProfile Player { get; set; }

        ApexChart<RankHistoryRecord> RankHistoryChart;

        ApexChartOptions<RankHistoryRecord> RankHistoryChartOptions = new()
        {
            Annotations = new(),
            Chart = new()
            {
                Background = "transparent",
                ForeColor = MapMavenTheme.Theme.PaletteDark.TextPrimary.SetAlpha(0.3).ToString(MudColorOutputFormats.RGBA),
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
                BorderColor = MapMavenTheme.Theme.PaletteDark.TextPrimary.SetAlpha(0.3).ToString(MudColorOutputFormats.RGBA),
            },
            Theme = new()
            {
                Mode = Mode.Dark,
            }
        };

        double? RankChange { get; set; }
        double? CountryRankChange { get; set; }
        double? PpChange { get; set; }

        double? TotalUnrankedPlays { get; set; }
        double? TotalRankedPlays { get; set; }
        double? TopPp { get; set; }
        double? AveragePp { get; set; }
        double? AverageStarDifficulty { get; set; }
        double? TopStarDifficulty { get; set; }
        double? AverageRankedAccuracy { get; set; }
        double? TopRankedAccuracy { get; set; }

        double? RecentAverageStarDifficulty { get; set; }
        string? RecentBestScoredMapTag { get; set; }


        IEnumerable<string> BestScoredMapTags { get; set; } = [];

        IEnumerable<Map> RecentHighPpGainMaps { get; set; } = [];
        IEnumerable<Map> RecommendedMaps { get; set; } = [];

        LivePlaylistConfiguration? RecommendedMapsLivePlaylistConfiguration { get; set; }

        bool ShowHighPpGainAnnotations { get; set; } = false;

        static readonly DateTime DashboardDateRangeMax = DateTime.Today.AddDays(1);
        static readonly DateTime DashboardDateRangeMin = DateTime.Today.AddDays(-50);

        static readonly DateRange PastFiftyDays = new(DashboardDateRangeMin, DashboardDateRangeMax);
        static readonly DateRange PastMonth = new(DateTime.Today.AddDays(-30), DashboardDateRangeMax);
        static readonly DateRange PastWeek = new(DateTime.Today.AddDays(-7), DashboardDateRangeMax);

        DateRange DashboardDateRange { get; set; }

        BehaviorSubject<DateRange> _dashboardDateRange = new(PastFiftyDays);

        IEnumerable<RankHistoryRecord> RankHistory { get; set; } = [];

        protected override void OnInitialized()
        {
            if (string.IsNullOrEmpty(LeaderboardService.PlayerId))
            {
                NavigationManager.NavigateTo("/maps"); // Start page should be the maps page if no leaderboard provider is set
                return;
            }

            var estimatingScoresObservable = Observable.CombineLatest(ScoreEstimationServices.Select(s => s.EstimatingScores), x => x.Any(estimatingScores => estimatingScores));

            var loadingObservable = Observable.CombineLatest(
                BeatSaberDataService.LoadingMapInfo,
                estimatingScoresObservable,
                (loadingMapInfo, estimatingScores) => loadingMapInfo || estimatingScores
            );

            SubscribeAndBind(loadingObservable, loading => LoadingDashboardData = loading);

            SubscribeAndBind(LeaderboardService.PlayerIdObservable.Zip(LeaderboardService.PlayerIdObservable.Skip(1), (previousPlayerId, playerId) => (previousPlayerId, playerId)), x =>
            {
                if (string.IsNullOrEmpty(x.playerId) && !string.IsNullOrEmpty(x.previousPlayerId))
                    NavigationManager.NavigateTo("/maps"); // Go to maps page if leaderboard provider is unset
            });

            SubscribeAndBind(_dashboardDateRange, range => DashboardDateRange = range);

            SubscribeAndBind(Observable.CombineLatest(LeaderboardService.PlayerProfile, _dashboardDateRange, (player, dateRange) => (player, dateRange)), x =>
            {
                if (x.player is null)
                    return;

                Player = x.player;

                RankHistory = x.player.RankHistory
                    .Where(h => h.Date >= DateOnly.FromDateTime(x.dateRange.Start.Value) && h.Date <= DateOnly.FromDateTime(x.dateRange.End.Value));

                var earliestRankHistoryRecord = RankHistory.FirstOrDefault();
                var latestRankHistoryRecord = RankHistory.LastOrDefault();

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

                if (rankedScores.Any())
                {
                    TotalRankedPlays = rankedScores.Count();

                    TopPp = rankedScores.Max(s => s.Score.Pp);

                    AveragePp = rankedScores
                        .OrderByDescending(s => s.Score.Pp)
                        .Take(30)
                        .Average(s => s.Score.Pp);

                    AverageStarDifficulty = rankedScores
                        .OrderByDescending(s => s.Score.Pp)
                        .Take(30)
                        .Average(s => s.Leaderboard.Stars);

                    TopStarDifficulty = rankedScores.Max(s => s.Leaderboard.Stars);

                    AverageRankedAccuracy = rankedScores
                        .OrderByDescending(s => s.Score.Pp)
                        .Average(s => s.Score.Accuracy);

                    TopRankedAccuracy = rankedScores.Max(s => s.Score.Accuracy);
                }

                var unrankedScores = scores.Where(x => x.Leaderboard.Stars == 0);

                TotalUnrankedPlays = unrankedScores.Count();
            });

            var mapsAndHistory = Observable.CombineLatest(MapService.RankedMaps, LeaderboardService.PlayerProfile, _dashboardDateRange, (rankedMaps, playerProfile, dateRange) => (rankedMaps, playerProfile, dateRange));

            SubscribeAndBind(mapsAndHistory, x =>
            {
                if (x.playerProfile is null)
                    return;

                var rangeStartDate = DateOnly.FromDateTime(x.dateRange.Start.Value);
                var rangeStartEnd = DateOnly.FromDateTime(x.dateRange.End.Value);

                var playedRankedMaps = x.rankedMaps.Where(m => m.Played);

                BestScoredMapTags = playedRankedMaps
                    .OrderByDescending(m => m.HighestPlayerScore?.Score.Pp ?? 0)
                    .SelectMany(m => m.Tags)
                    .GroupBy(t => t)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key);

                var recentPlayedRankedMaps = playedRankedMaps
                    .Where(m => m.HighestPlayerScore is not null && m.HighestPlayerScore.Score.TimeSet >= x.dateRange.Start && m.HighestPlayerScore.Score.TimeSet <= x.dateRange.End)
                    .OrderBy(m => m.HighestPlayerScore.Score.TimeSet);

                var rankHistoryInRange = x.playerProfile.RankHistory
                    .Where(h => h.Date >= rangeStartDate && h.Date <= rangeStartEnd);

                var rankIncreaseHistoryRecords = rankHistoryInRange
                    .Where(h => h.Date >= rangeStartDate && h.Date <= rangeStartEnd)
                    .Zip(rankHistoryInRange.Skip(1), (previousHistoryRecord, historyRecord) => (previousHistoryRecord, historyRecord))
                    .Where(h => h.historyRecord.Rank < h.previousHistoryRecord.Rank)
                    .Select(h => h.historyRecord);

                RecentHighPpGainMaps = rankIncreaseHistoryRecords
                    .SelectMany(historyRecord => recentPlayedRankedMaps
                        .Where(map =>
                        {
                            var scoreDate = DateOnly.FromDateTime(map.HighestPlayerScore.Score.TimeSet.DateTime);

                            return scoreDate >= historyRecord.Date.AddDays(-1) && scoreDate <= historyRecord.Date.AddDays(1);
                        })
                    ).Distinct()
                    .OrderByDescending(m => m.HighestPlayerScore.Score.WeightedPp);

                SetHighPpGainAnnotations();

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

                RecommendedMapsLivePlaylistConfiguration = new LivePlaylistConfiguration
                {
                    MapCount = 20,
                    MapPool = MapPool.Improvement,
                    FilterOperations = new()
                    {
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Stars),
                            Operator = Core.Models.LivePlaylists.FilterOperator.GreaterThanOrEqual,
                            Value = RecentAverageStarDifficulty?.ToString(CultureInfo.InvariantCulture) ?? "0"
                        },
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Tags),
                            Operator = Core.Models.LivePlaylists.FilterOperator.Contains,
                            Value = RecentBestScoredMapTag
                        },
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Hidden),
                            Operator = Core.Models.LivePlaylists.FilterOperator.Equals,
                            Value = "false"
                        },
                        new FilterOperation
                        {
                            Field = nameof(AdvancedSearchMap.Played),
                            Operator = Core.Models.LivePlaylists.FilterOperator.Equals,
                            Value = "false"
                        },
                        new FilterOperation
                        {
                            Field = $"{nameof(AdvancedSearchMap.ScoreEstimate)}.{nameof(AdvancedSearchMap.ScoreEstimate.PPIncrease)}",
                            Operator = Core.Models.LivePlaylists.FilterOperator.GreaterThanOrEqual,
                            Value = "0"
                        },
                        new FilterOperation
                        {
                            Field = $"{nameof(AdvancedSearchMap.ScoreEstimate)}.{nameof(AdvancedSearchMap.ScoreEstimate.Accuracy)}",
                            Operator = Core.Models.LivePlaylists.FilterOperator.GreaterThanOrEqual,
                            Value = 80.ToString()
                        }
                    },
                    SortOperations = new()
                    {
                        new SortOperation
                        {
                            Field = $"{nameof(AdvancedSearchMap.ScoreEstimate)}.{nameof(AdvancedSearchMap.ScoreEstimate.PPIncrease)}",
                            Direction = Core.Models.LivePlaylists.SortDirection.Descending
                        }
                    }
                };

                RecommendedMaps = x.rankedMaps;

                foreach (var filterOperation in RecommendedMapsLivePlaylistConfiguration.FilterOperations)
                {
                    RecommendedMaps = RecommendedMaps.Where(map => MapSearchService.FilterOperationMatches(new AdvancedSearchMap(map), filterOperation));
                }

                RecommendedMaps = MapSearchService.SortMaps(RecommendedMaps, RecommendedMapsLivePlaylistConfiguration.SortOperations, x => new AdvancedSearchMap(x));

                RecommendedMaps = RecommendedMaps.Take(20);

                UpdateChartOptions();
            });
        }

        private void UpdateChartOptions()
        {
            InvokeAsync(async () =>
            {
                if (RankHistoryChart is null)
                    return;

                await RankHistoryChart.UpdateOptionsAsync(true, false, false);
                StateHasChanged();
            });
        }

        private void SetHighPpGainAnnotations()
        {
            if (ShowHighPpGainAnnotations)
            {
                RankHistoryChartOptions.Annotations.Xaxis = RecentHighPpGainMaps
                .GroupBy(m => DateOnly.FromDateTime(m.HighestPlayerScore.Score.TimeSet.DateTime))
                .Select(r => new AnnotationsXAxis()
                {
                    X = r.Key.ToString("yyyy-MM-dd"),
                    StrokeDashArray = 0,
                    BorderColor = MapMavenTheme.Theme.PaletteDark.TextPrimary.SetAlpha(0.3).ToString(MudColorOutputFormats.RGBA),
                    Label = new()
                    {
                        Orientation = ApexCharts.Orientation.Horizontal,
                        BorderWidth = 0,
                        Style = new()
                        {
                            Background = MapMavenTheme.Theme.PaletteDark.Primary.ColorLighten(0.2).ToString(MudColorOutputFormats.Hex),
                            Color = "#FFF"
                        },
                        Text = "+pp"
                    }
                }).ToList();
            }
            else
            {
                RankHistoryChartOptions.Annotations.Xaxis = [];
            }

            UpdateChartOptions();
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
                        NavigationManager.NavigateTo("/maps");
                        return Task.CompletedTask;
                    };
                });
            }
            else
            {
                Snackbar.Add($"Canceled creating playlist.", Severity.Normal, config => config.Icon = Icons.Material.Filled.Cancel);
            }
        }

        async Task AddRecommendedMapsLivePlaylist()
        {
            var parameters = new DialogParameters
            {
                { "SelectedPlaylist", new EditLivePlaylistModel { LivePlaylistConfiguration = DeepCloner.Clone(RecommendedMapsLivePlaylistConfiguration)} }
            };

            DialogService.Show<EditLivePlaylistDialog>("Add playlist", parameters, new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            });
        }

        void OnDateRangeChanged() => _dashboardDateRange.OnNext(DashboardDateRange);
        void OnDateRangeChanged(DateRange value)
        {
            DashboardDateRange = value;
            _dashboardDateRange.OnNext(DashboardDateRange);
        }
    }
}
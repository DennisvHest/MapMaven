using ApexCharts;
using MapMaven.Core.Models;
using MapMaven.Core.Services.Leaderboards;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;

namespace MapMaven.Pages
{
    public partial class Dashboard
    {
        [Inject]
        ILeaderboardService LeaderboardService { get; set; }

        IEnumerable<RankHistoryRecord> RankHistory { get; set; }

        ApexChart<RankHistoryRecord> RankHistoryChart;

        ApexChartOptions<RankHistoryRecord> RankHistoryChartOptions = new()
        {
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
            Grid = new()
            {
                BorderColor = MapMavenTheme.Theme.Palette.TextPrimary.SetAlpha(0.3).ToString(MudColorOutputFormats.RGBA),
            },
            Theme = new()
            {
                Mode = Mode.Dark,
            }
        };

        protected override void OnInitialized()
        {
            SubscribeAndBind(LeaderboardService.PlayerProfile, player =>
            {
                RankHistory = player.RankHistory;

                InvokeAsync(async () =>
                {
                    if (RankHistoryChart is null)
                        return;

                    await RankHistoryChart.UpdateSeriesAsync();
                    StateHasChanged();
                });
            });
        }
    }
}
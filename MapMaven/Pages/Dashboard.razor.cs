using MapMaven.Core.Services.Leaderboards;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MapMaven.Pages
{
    public partial class Dashboard
    {
        [Inject]
        ILeaderboardService LeaderboardService { get; set; }

        ChartOptions ScoresChartOptions = new();

        List<ChartSeries> ScoresSeries { get; set; }
        string[] ScoresXAxisLabels { get; set; }


        protected override void OnInitialized()
        {
            ScoresXAxisLabels = Enumerable.Range(1, 30)
                .Select(offset => DateTime.Now.Date.AddDays(-offset).ToShortDateString())
                .ToArray();

            SubscribeAndBind(LeaderboardService.PlayerProfile, player =>
            {
                ScoresSeries = [
                    new()
                    {
                        Name = "Scores",
                        Data = player.RankHistory
                            .Select(h => Convert.ToDouble(h.Rank))
                            .Reverse()
                            .ToArray()
                    }
                ];
            });
        }
    }
}
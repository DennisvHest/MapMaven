using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapMaven.DataGatherers.BeatLeader.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerScoreStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<long>(type: "bigint", nullable: false),
                    TotalUnrankedScore = table.Column<long>(type: "bigint", nullable: false),
                    TotalRankedScore = table.Column<long>(type: "bigint", nullable: false),
                    LastScoreTime = table.Column<int>(type: "int", nullable: false),
                    LastUnrankedScoreTime = table.Column<int>(type: "int", nullable: false),
                    LastRankedScoreTime = table.Column<int>(type: "int", nullable: false),
                    AverageRankedAccuracy = table.Column<float>(type: "real", nullable: false),
                    AverageWeightedRankedAccuracy = table.Column<float>(type: "real", nullable: false),
                    AverageUnrankedAccuracy = table.Column<float>(type: "real", nullable: false),
                    AverageAccuracy = table.Column<float>(type: "real", nullable: false),
                    MedianRankedAccuracy = table.Column<float>(type: "real", nullable: false),
                    MedianAccuracy = table.Column<float>(type: "real", nullable: false),
                    TopRankedAccuracy = table.Column<float>(type: "real", nullable: false),
                    TopUnrankedAccuracy = table.Column<float>(type: "real", nullable: false),
                    TopAccuracy = table.Column<float>(type: "real", nullable: false),
                    TopPp = table.Column<float>(type: "real", nullable: false),
                    TopBonusPP = table.Column<float>(type: "real", nullable: false),
                    TopPassPP = table.Column<float>(type: "real", nullable: false),
                    TopAccPP = table.Column<float>(type: "real", nullable: false),
                    TopTechPP = table.Column<float>(type: "real", nullable: false),
                    PeakRank = table.Column<float>(type: "real", nullable: false),
                    RankedMaxStreak = table.Column<int>(type: "int", nullable: false),
                    UnrankedMaxStreak = table.Column<int>(type: "int", nullable: false),
                    MaxStreak = table.Column<int>(type: "int", nullable: false),
                    AverageLeftTiming = table.Column<float>(type: "real", nullable: false),
                    AverageRightTiming = table.Column<float>(type: "real", nullable: false),
                    RankedPlayCount = table.Column<int>(type: "int", nullable: false),
                    UnrankedPlayCount = table.Column<int>(type: "int", nullable: false),
                    TotalPlayCount = table.Column<int>(type: "int", nullable: false),
                    RankedImprovementsCount = table.Column<int>(type: "int", nullable: false),
                    UnrankedImprovementsCount = table.Column<int>(type: "int", nullable: false),
                    TotalImprovementsCount = table.Column<int>(type: "int", nullable: false),
                    RankedTop1Count = table.Column<int>(type: "int", nullable: false),
                    UnrankedTop1Count = table.Column<int>(type: "int", nullable: false),
                    Top1Count = table.Column<int>(type: "int", nullable: false),
                    RankedTop1Score = table.Column<int>(type: "int", nullable: false),
                    UnrankedTop1Score = table.Column<int>(type: "int", nullable: false),
                    Top1Score = table.Column<int>(type: "int", nullable: false),
                    AverageRankedRank = table.Column<float>(type: "real", nullable: false),
                    AverageWeightedRankedRank = table.Column<float>(type: "real", nullable: false),
                    AverageUnrankedRank = table.Column<float>(type: "real", nullable: false),
                    AverageRank = table.Column<float>(type: "real", nullable: false),
                    SspPlays = table.Column<int>(type: "int", nullable: false),
                    SsPlays = table.Column<int>(type: "int", nullable: false),
                    SpPlays = table.Column<int>(type: "int", nullable: false),
                    SPlays = table.Column<int>(type: "int", nullable: false),
                    APlays = table.Column<int>(type: "int", nullable: false),
                    TopPlatform = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopHMD = table.Column<int>(type: "int", nullable: false),
                    DailyImprovements = table.Column<int>(type: "int", nullable: false),
                    AuthorizedReplayWatched = table.Column<int>(type: "int", nullable: false),
                    AnonimusReplayWatched = table.Column<int>(type: "int", nullable: false),
                    WatchedReplays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerScoreStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Platform = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bot = table.Column<bool>(type: "bit", nullable: false),
                    Pp = table.Column<float>(type: "real", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    CountryRank = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccPp = table.Column<float>(type: "real", nullable: false),
                    PassPp = table.Column<float>(type: "real", nullable: false),
                    TechPp = table.Column<float>(type: "real", nullable: false),
                    ScoreStatsId = table.Column<int>(type: "int", nullable: true),
                    LastWeekPp = table.Column<float>(type: "real", nullable: false),
                    LastWeekRank = table.Column<int>(type: "int", nullable: false),
                    LastWeekCountryRank = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_PlayerScoreStats_ScoreStatsId",
                        column: x => x.ScoreStatsId,
                        principalTable: "PlayerScoreStats",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_ScoreStatsId",
                table: "Players",
                column: "ScoreStatsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "PlayerScoreStats");
        }
    }
}

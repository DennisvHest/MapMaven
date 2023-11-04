using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapMaven.DataGatherers.BeatLeader.Migrations
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
                name: "Songs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mapper = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MapperId = table.Column<int>(type: "int", nullable: false),
                    CoverImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullCoverImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DownloadUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bpm = table.Column<double>(type: "float", nullable: false),
                    Duration = table.Column<double>(type: "float", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadTime = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Songs", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "Leaderboards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Difficulty_Id = table.Column<int>(type: "int", nullable: true),
                    Difficulty_Value = table.Column<int>(type: "int", nullable: true),
                    Difficulty_Mode = table.Column<int>(type: "int", nullable: true),
                    Difficulty_DifficultyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Difficulty_ModeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Difficulty_Status = table.Column<int>(type: "int", nullable: true),
                    Difficulty_ModifierValues_ModifierId = table.Column<int>(type: "int", nullable: true),
                    Difficulty_ModifierValues_Da = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Fs = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Sf = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Ss = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Gn = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Na = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Nb = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Nf = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_No = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Pm = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Sc = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Sa = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifierValues_Op = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_Id = table.Column<int>(type: "int", nullable: true),
                    Difficulty_ModifiersRating_FsPredictedAcc = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_FsPassRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_FsAccRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_FsTechRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_FsStars = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SsPredictedAcc = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SsPassRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SsAccRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SsTechRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SsStars = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SfPredictedAcc = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SfPassRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SfAccRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SfTechRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_ModifiersRating_SfStars = table.Column<float>(type: "real", nullable: true),
                    Difficulty_NominatedTime = table.Column<int>(type: "int", nullable: true),
                    Difficulty_QualifiedTime = table.Column<int>(type: "int", nullable: true),
                    Difficulty_RankedTime = table.Column<int>(type: "int", nullable: true),
                    Difficulty_Stars = table.Column<float>(type: "real", nullable: true),
                    Difficulty_PredictedAcc = table.Column<float>(type: "real", nullable: true),
                    Difficulty_PassRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_AccRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_TechRating = table.Column<float>(type: "real", nullable: true),
                    Difficulty_Type = table.Column<int>(type: "int", nullable: true),
                    Difficulty_Njs = table.Column<float>(type: "real", nullable: true),
                    Difficulty_Nps = table.Column<float>(type: "real", nullable: true),
                    Difficulty_Notes = table.Column<int>(type: "int", nullable: true),
                    Difficulty_Bombs = table.Column<int>(type: "int", nullable: true),
                    Difficulty_Walls = table.Column<int>(type: "int", nullable: true),
                    Difficulty_MaxScore = table.Column<int>(type: "int", nullable: true),
                    Difficulty_Duration = table.Column<double>(type: "float", nullable: true),
                    Difficulty_Requirements = table.Column<int>(type: "int", nullable: true),
                    Plays = table.Column<int>(type: "int", nullable: false),
                    ClanRankingContested = table.Column<bool>(type: "bit", nullable: false),
                    SongId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leaderboards_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BaseScore = table.Column<int>(type: "int", nullable: false),
                    ModifiedScore = table.Column<int>(type: "int", nullable: false),
                    Accuracy = table.Column<float>(type: "real", nullable: false),
                    PlayerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Pp = table.Column<float>(type: "real", nullable: false),
                    BonusPp = table.Column<float>(type: "real", nullable: false),
                    PassPP = table.Column<float>(type: "real", nullable: false),
                    AccPP = table.Column<float>(type: "real", nullable: false),
                    TechPP = table.Column<float>(type: "real", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FcAccuracy = table.Column<float>(type: "real", nullable: false),
                    FcPp = table.Column<float>(type: "real", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    Replay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Modifiers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BadCuts = table.Column<int>(type: "int", nullable: false),
                    MissedNotes = table.Column<int>(type: "int", nullable: false),
                    BombCuts = table.Column<int>(type: "int", nullable: false),
                    WallsHit = table.Column<int>(type: "int", nullable: false),
                    Pauses = table.Column<int>(type: "int", nullable: false),
                    FullCombo = table.Column<bool>(type: "bit", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxCombo = table.Column<int>(type: "int", nullable: false),
                    MaxStreak = table.Column<int>(type: "int", nullable: true),
                    Hmd = table.Column<int>(type: "int", nullable: false),
                    Controller = table.Column<int>(type: "int", nullable: false),
                    LeaderboardId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Timeset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timepost = table.Column<int>(type: "int", nullable: false),
                    ReplaysWatched = table.Column<int>(type: "int", nullable: false),
                    PlayCount = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AccLeft = table.Column<float>(type: "real", nullable: false),
                    AccRight = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scores_Leaderboards_LeaderboardId",
                        column: x => x.LeaderboardId,
                        principalTable: "Leaderboards",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Scores_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_SongId",
                table: "Leaderboards",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ScoreStatsId",
                table: "Players",
                column: "ScoreStatsId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_LeaderboardId",
                table: "Scores",
                column: "LeaderboardId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_PlayerId",
                table: "Scores",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "Leaderboards");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Songs");

            migrationBuilder.DropTable(
                name: "PlayerScoreStats");
        }
    }
}

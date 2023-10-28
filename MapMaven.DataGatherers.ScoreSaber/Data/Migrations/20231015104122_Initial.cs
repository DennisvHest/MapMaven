using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapMaven.DataGatherers.ScoreSaber.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pp = table.Column<double>(type: "float", nullable: false),
                    Rank = table.Column<double>(type: "float", nullable: false),
                    CountryRank = table.Column<double>(type: "float", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Histories = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalScore = table.Column<double>(type: "float", nullable: false),
                    TotalRankedScore = table.Column<double>(type: "float", nullable: false),
                    AverageRankedAccuracy = table.Column<double>(type: "float", nullable: false),
                    TotalPlayCount = table.Column<double>(type: "float", nullable: false),
                    RankedPlayCount = table.Column<double>(type: "float", nullable: false),
                    ReplaysWatched = table.Column<double>(type: "float", nullable: false),
                    Permissions = table.Column<double>(type: "float", nullable: false),
                    Banned = table.Column<bool>(type: "bit", nullable: false),
                    Inactive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<double>(type: "float", nullable: false),
                    Rank = table.Column<double>(type: "float", nullable: false),
                    BaseScore = table.Column<double>(type: "float", nullable: false),
                    ModifiedScore = table.Column<double>(type: "float", nullable: false),
                    Pp = table.Column<double>(type: "float", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Modifiers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Multiplier = table.Column<double>(type: "float", nullable: false),
                    BadCuts = table.Column<double>(type: "float", nullable: false),
                    MissedNotes = table.Column<double>(type: "float", nullable: false),
                    MaxCombo = table.Column<double>(type: "float", nullable: false),
                    FullCombo = table.Column<bool>(type: "bit", nullable: false),
                    Hmd = table.Column<double>(type: "float", nullable: false),
                    HasReplay = table.Column<bool>(type: "bit", nullable: false),
                    TimeSet = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Difficulty",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaderboardId = table.Column<double>(type: "float", nullable: false),
                    Difficulty1 = table.Column<double>(type: "float", nullable: false),
                    GameMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DifficultyRaw = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaderboardInfoId = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Difficulty", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leaderboards",
                columns: table => new
                {
                    Id = table.Column<double>(type: "float", nullable: false),
                    SongHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SongName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SongSubName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SongAuthorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LevelAuthorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DifficultyId = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<double>(type: "float", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RankedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    QualifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LovedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Ranked = table.Column<bool>(type: "bit", nullable: false),
                    Qualified = table.Column<bool>(type: "bit", nullable: false),
                    Loved = table.Column<bool>(type: "bit", nullable: false),
                    MaxPP = table.Column<double>(type: "float", nullable: false),
                    Stars = table.Column<double>(type: "float", nullable: false),
                    PositiveModifiers = table.Column<bool>(type: "bit", nullable: false),
                    Plays = table.Column<double>(type: "float", nullable: false),
                    DailyPlays = table.Column<double>(type: "float", nullable: false),
                    CoverImage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leaderboards_Difficulty_DifficultyId",
                        column: x => x.DifficultyId,
                        principalTable: "Difficulty",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScoreId = table.Column<double>(type: "float", nullable: false),
                    LeaderboardId = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerScores_Leaderboards_LeaderboardId",
                        column: x => x.LeaderboardId,
                        principalTable: "Leaderboards",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayerScores_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerScores_Scores_ScoreId",
                        column: x => x.ScoreId,
                        principalTable: "Scores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Difficulty_LeaderboardInfoId",
                table: "Difficulty",
                column: "LeaderboardInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_DifficultyId",
                table: "Leaderboards",
                column: "DifficultyId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerScores_LeaderboardId",
                table: "PlayerScores",
                column: "LeaderboardId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerScores_PlayerId",
                table: "PlayerScores",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerScores_ScoreId",
                table: "PlayerScores",
                column: "ScoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Difficulty_Leaderboards_LeaderboardInfoId",
                table: "Difficulty",
                column: "LeaderboardInfoId",
                principalTable: "Leaderboards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Difficulty_Leaderboards_LeaderboardInfoId",
                table: "Difficulty");

            migrationBuilder.DropTable(
                name: "PlayerScores");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "Leaderboards");

            migrationBuilder.DropTable(
                name: "Difficulty");
        }
    }
}

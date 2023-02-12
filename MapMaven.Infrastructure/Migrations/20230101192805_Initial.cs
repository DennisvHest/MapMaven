using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapMaven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MapInfos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    DirectoryPath = table.Column<string>(type: "TEXT", nullable: false),
                    SongName = table.Column<string>(type: "TEXT", nullable: false),
                    SongAuthorName = table.Column<string>(type: "TEXT", nullable: false),
                    LevelAuthorName = table.Column<string>(type: "TEXT", nullable: false),
                    SongFileName = table.Column<string>(type: "TEXT", nullable: false),
                    CoverImageFilename = table.Column<string>(type: "TEXT", nullable: false),
                    AddedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SongDuration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    PreviewStartTimeInSeconds = table.Column<float>(type: "REAL", nullable: false),
                    PreviewDurationInSeconds = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapInfos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapInfos");
        }
    }
}

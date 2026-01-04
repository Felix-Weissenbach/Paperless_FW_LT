using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paperless.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyAccessCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyAccessCount",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyAccessCount",
                table: "documents");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paperless.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "documents");
        }
    }
}

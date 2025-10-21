using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paperless.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "documents");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "documents",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "documents",
                newName: "UploadedAt");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "StoragePath",
                table: "documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "StoragePath",
                table: "documents");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "documents",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "documents",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

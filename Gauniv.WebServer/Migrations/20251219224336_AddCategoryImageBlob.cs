using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gauniv.WebServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryImageBlob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "Categories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Categories",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Categories");
        }
    }
}

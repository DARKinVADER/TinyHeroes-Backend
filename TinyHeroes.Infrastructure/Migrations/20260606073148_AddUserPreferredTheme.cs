using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyHeroes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferredTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredTheme",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "sunny");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredTheme",
                table: "AspNetUsers");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyHeroes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResolvedByFKToJoinRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FamilyJoinRequests_ResolvedById",
                table: "FamilyJoinRequests",
                column: "ResolvedById");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyJoinRequests_AspNetUsers_ResolvedById",
                table: "FamilyJoinRequests",
                column: "ResolvedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyJoinRequests_AspNetUsers_ResolvedById",
                table: "FamilyJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_FamilyJoinRequests_ResolvedById",
                table: "FamilyJoinRequests");
        }
    }
}

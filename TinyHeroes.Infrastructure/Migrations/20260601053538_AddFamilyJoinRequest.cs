using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyHeroes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyJoinRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "Families",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            // Backfill unique codes before creating the unique index.
            // Existing rows all have JoinCode = "" which would violate the constraint.
            migrationBuilder.Sql(@"
                UPDATE ""Families""
                SET ""JoinCode"" = upper(substring(md5(""Id""::text || random()::text), 1, 8))
                WHERE ""JoinCode"" = '';
            ");

            migrationBuilder.CreateTable(
                name: "FamilyJoinRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyJoinRequests_AspNetUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyJoinRequests_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Families_JoinCode",
                table: "Families",
                column: "JoinCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyJoinRequests_FamilyId",
                table: "FamilyJoinRequests",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyJoinRequests_RequestedById_Status",
                table: "FamilyJoinRequests",
                columns: new[] { "RequestedById", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_Families_JoinCode",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "Families");
        }
    }
}

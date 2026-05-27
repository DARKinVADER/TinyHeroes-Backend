using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyHeroes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrizeClaimsAndComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrizeClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    WeekSummaryId = table.Column<Guid>(type: "uuid", nullable: true),
                    MonthSummaryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rank = table.Column<int>(type: "integer", nullable: true),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrizeEmoji = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrizeLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrizeClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrizeClaims_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrizeClaims_MonthSummaries_MonthSummaryId",
                        column: x => x.MonthSummaryId,
                        principalTable: "MonthSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrizeClaims_WeekSummaries_WeekSummaryId",
                        column: x => x.WeekSummaryId,
                        principalTable: "WeekSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrizeComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrizeClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrizeComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrizeComments_PrizeClaims_PrizeClaimId",
                        column: x => x.PrizeClaimId,
                        principalTable: "PrizeClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrizeClaims_FamilyId_Scope_WeekSummaryId_MonthSummaryId_Rank",
                table: "PrizeClaims",
                columns: new[] { "FamilyId", "Scope", "WeekSummaryId", "MonthSummaryId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrizeClaims_MonthSummaryId",
                table: "PrizeClaims",
                column: "MonthSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeClaims_WeekSummaryId",
                table: "PrizeClaims",
                column: "WeekSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeComments_PrizeClaimId",
                table: "PrizeComments",
                column: "PrizeClaimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrizeComments");

            migrationBuilder.DropTable(
                name: "PrizeClaims");
        }
    }
}

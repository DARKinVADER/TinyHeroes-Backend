using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyHeroes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeedPresetLabelKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelKey",
                table: "DeedPresets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "LabelKey",
                value: "PRESET.SYSTEM.DID_HOMEWORK");

            migrationBuilder.UpdateData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "LabelKey",
                value: "PRESET.SYSTEM.HELPED_IN_KITCHEN");

            migrationBuilder.UpdateData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "LabelKey",
                value: "PRESET.SYSTEM.CLEANED_ROOM");

            migrationBuilder.UpdateData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "LabelKey",
                value: "PRESET.SYSTEM.HELPED_SIBLING");

            migrationBuilder.UpdateData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "LabelKey",
                value: "PRESET.SYSTEM.BEHAVED_ALL_DAY");

            migrationBuilder.UpdateData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "LabelKey",
                value: "PRESET.SYSTEM.MADE_BED");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabelKey",
                table: "DeedPresets");
        }
    }
}

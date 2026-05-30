using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TinyHeroes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewDeedPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DeedPresets",
                columns: new[] { "Id", "CreatedAt", "Enabled", "FamilyId", "ImageValue", "Label", "LabelKey" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000007"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "👕", "Put clothes on", "PRESET.SYSTEM.PUT_CLOTHES_ON" },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "🫧", "Put clothes in washing machine", "PRESET.SYSTEM.PUT_CLOTHES_IN_WASHING_MACHINE" },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "🌀", "Put clothes in dryer", "PRESET.SYSTEM.PUT_CLOTHES_IN_DRYER" },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "🍽️", "Put plates in dishwasher", "PRESET.SYSTEM.PUT_PLATES_IN_DISHWASHER" },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "⚽", "Played football", "PRESET.SYSTEM.PLAYED_FOOTBALL" },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "🤸", "Did gymnastics", "PRESET.SYSTEM.DID_GYMNASTICS" },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "🏊", "Went swimming", "PRESET.SYSTEM.WENT_SWIMMING" },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "🦷", "Brushed teeth", "PRESET.SYSTEM.BRUSHED_TEETH" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "DeedPresets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000014"));
        }
    }
}

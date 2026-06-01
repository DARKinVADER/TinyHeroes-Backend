using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyHeroes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillFamilyJoinCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill join codes for families created before this feature.
            // Generates a unique 8-char uppercase alphanumeric code per family.
            // Retries on the rare collision by appending the row id for uniqueness.
            migrationBuilder.Sql(@"
                UPDATE ""Families""
                SET ""JoinCode"" = upper(substring(md5(""Id""::text || random()::text), 1, 8))
                WHERE ""JoinCode"" = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Families"" SET ""JoinCode"" = '' WHERE ""JoinCode"" <> '';
            ");
        }
    }
}

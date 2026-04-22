using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingJournal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowedSectionsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column with a default empty array so existing rows are handled
            migrationBuilder.Sql("ALTER TABLE \"Users\" ADD \"AllowedSections\" text[] NOT NULL DEFAULT '{}';");
            // Remove the default so future rows must be explicitly set
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"AllowedSections\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedSections",
                table: "Users");
        }
    }
}

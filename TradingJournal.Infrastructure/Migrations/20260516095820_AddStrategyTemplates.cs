using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingJournal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStrategyTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StrategyTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Methodology = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Instrument = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rules = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultFilters = table.Column<string>(type: "text", nullable: false),
                    SessionBadge = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TimeframeBadge = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinRRR = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyTemplates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StrategyTemplates_UserId",
                table: "StrategyTemplates",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StrategyTemplates");
        }
    }
}

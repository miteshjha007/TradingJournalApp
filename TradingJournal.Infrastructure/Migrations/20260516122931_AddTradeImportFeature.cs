using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingJournal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeImportFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Mt5TicketNumber",
                table: "Trades",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Mt5WebhookConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DefaultTradingAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultInstrumentMappings = table.Column<string>(type: "text", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalTradesImported = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mt5WebhookConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mt5WebhookConfigs_TradingAccounts_DefaultTradingAccountId",
                        column: x => x.DefaultTradingAccountId,
                        principalTable: "TradingAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Mt5WebhookConfigs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeImportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    TotalReceived = table.Column<int>(type: "integer", nullable: false),
                    TotalInserted = table.Column<int>(type: "integer", nullable: false),
                    TotalSkipped = table.Column<int>(type: "integer", nullable: false),
                    TotalFailed = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true),
                    InsertedTradeIds = table.Column<string>(type: "text", nullable: true),
                    SkippedReasons = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeImportLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeImportLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mt5WebhookConfigs_DefaultTradingAccountId",
                table: "Mt5WebhookConfigs",
                column: "DefaultTradingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Mt5WebhookConfigs_UserId",
                table: "Mt5WebhookConfigs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Mt5WebhookConfigs_WebhookToken",
                table: "Mt5WebhookConfigs",
                column: "WebhookToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradeImportLogs_CreatedAt",
                table: "TradeImportLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TradeImportLogs_UserId",
                table: "TradeImportLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mt5WebhookConfigs");

            migrationBuilder.DropTable(
                name: "TradeImportLogs");

            migrationBuilder.DropColumn(
                name: "Mt5TicketNumber",
                table: "Trades");
        }
    }
}

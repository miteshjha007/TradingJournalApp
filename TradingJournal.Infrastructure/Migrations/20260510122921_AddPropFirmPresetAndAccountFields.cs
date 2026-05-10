using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingJournal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropFirmPresetAndAccountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "TradingAccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "MinTradingDays",
                table: "TradingAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "NewsTradeAllowed",
                table: "TradingAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PropFirmName",
                table: "TradingAccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropFirmPlan",
                table: "TradingAccounts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WeekendHoldingAllowed",
                table: "TradingAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PropFirmPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirmName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlanName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AccountSize = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DailyDrawdownLimitPct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MaxOverallLossPct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProfitTargetPct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProfitSplitPct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MinTradingDays = table.Column<int>(type: "integer", nullable: false),
                    MaxAllowedLotSize = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Has5xLotRule = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRiskPerTradePctOfDailyLimit = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    UseDynamicEquity = table.Column<bool>(type: "boolean", nullable: false),
                    NewsTradeAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    WeekendHoldingAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropFirmPresets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropFirmPresets_FirmName_PlanName",
                table: "PropFirmPresets",
                columns: new[] { "FirmName", "PlanName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropFirmPresets");

            migrationBuilder.DropColumn(
                name: "MinTradingDays",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "NewsTradeAllowed",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "PropFirmName",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "PropFirmPlan",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "WeekendHoldingAllowed",
                table: "TradingAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "TradingAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}

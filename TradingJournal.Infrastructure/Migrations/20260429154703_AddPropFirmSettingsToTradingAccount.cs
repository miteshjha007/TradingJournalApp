using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingJournal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropFirmSettingsToTradingAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyDrawdownLimitPct",
                table: "TradingAccounts",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "Has5xLotRule",
                table: "TradingAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPropFirm",
                table: "TradingAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxAllowedLotSize",
                table: "TradingAccounts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxOverallLossPct",
                table: "TradingAccounts",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxRiskPerTradePctOfDailyLimit",
                table: "TradingAccounts",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitSplitPct",
                table: "TradingAccounts",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitTargetPct",
                table: "TradingAccounts",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "UseDynamicEquity",
                table: "TradingAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyDrawdownLimitPct",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "Has5xLotRule",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "IsPropFirm",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "MaxAllowedLotSize",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "MaxOverallLossPct",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "MaxRiskPerTradePctOfDailyLimit",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "ProfitSplitPct",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "ProfitTargetPct",
                table: "TradingAccounts");

            migrationBuilder.DropColumn(
                name: "UseDynamicEquity",
                table: "TradingAccounts");
        }
    }
}

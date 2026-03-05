using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Infrastructure.FundData.Migrations
{
    /// <inheritdoc />
    public partial class InitialFundData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundProfiles",
                columns: table => new
                {
                    Isin = table.Column<string>(type: "NCHAR(12)", fixedLength: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OrderbookId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FundType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsIndexFund = table.Column<bool>(type: "bit", nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ManagedType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "DATE", nullable: true),
                    Buyable = table.Column<bool>(type: "bit", nullable: true),
                    HasCashDividends = table.Column<bool>(type: "bit", nullable: true),
                    HasCurrencyExchangeFee = table.Column<bool>(type: "bit", nullable: true),
                    RecommendedHoldingPeriod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ManagementFee = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    TotalFee = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    TransactionFee = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    OngoingFee = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    MinimumBuy = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: true),
                    Capital = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: true),
                    NumberOfOwners = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Risk = table.Column<int>(type: "int", nullable: true),
                    SharpeRatio = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    StandardDeviation = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    SustainabilityLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SustainabilityRating = table.Column<int>(type: "int", nullable: true),
                    EsgScore = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    EnvironmentalScore = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    SocialScore = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    GovernanceScore = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    LowCarbon = table.Column<bool>(type: "bit", nullable: true),
                    EuArticleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CrawlerLastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AboutFundLastVisitedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundProfiles", x => x.Isin);
                });

            migrationBuilder.CreateTable(
                name: "FundHistoryRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<string>(type: "NCHAR(12)", fixedLength: true, nullable: false),
                    Nav = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    NavDate = table.Column<DateOnly>(type: "DATE", nullable: true),
                    Capital = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: true),
                    NumberOfOwners = table.Column<int>(type: "int", nullable: true),
                    Risk = table.Column<int>(type: "int", nullable: true),
                    SharpeRatio = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true),
                    StandardDeviation = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundHistoryRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundHistoryRecords_FundProfiles_FundId",
                        column: x => x.FundId,
                        principalTable: "FundProfiles",
                        principalColumn: "Isin",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_FundHistoryRecords_FundId_NavDate",
                table: "FundHistoryRecords",
                columns: new[] { "FundId", "NavDate" },
                unique: true,
                descending: new[] { false, true },
                filter: "[NavDate] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundHistoryRecords");

            migrationBuilder.DropTable(
                name: "FundProfiles");
        }
    }
}

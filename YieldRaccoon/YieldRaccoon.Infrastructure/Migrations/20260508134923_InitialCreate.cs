using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable


namespace YieldRaccoon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundProfiles",
                columns: table => new
                {
                    Isin = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OrderbookId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    FundType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsIndexFund = table.Column<bool>(type: "INTEGER", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ManagedType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "TEXT", maxLength: 10, nullable: true),
                    Buyable = table.Column<bool>(type: "INTEGER", nullable: true),
                    HasCashDividends = table.Column<bool>(type: "INTEGER", nullable: true),
                    HasCurrencyExchangeFee = table.Column<bool>(type: "INTEGER", nullable: true),
                    RecommendedHoldingPeriod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ManagementFee = table.Column<decimal>(type: "REAL", nullable: true),
                    TotalFee = table.Column<decimal>(type: "REAL", nullable: true),
                    TransactionFee = table.Column<decimal>(type: "REAL", nullable: true),
                    OngoingFee = table.Column<decimal>(type: "REAL", nullable: true),
                    MinimumBuy = table.Column<decimal>(type: "REAL", nullable: true),
                    Capital = table.Column<decimal>(type: "REAL", nullable: true),
                    NumberOfOwners = table.Column<int>(type: "INTEGER", nullable: true),
                    Rating = table.Column<int>(type: "INTEGER", nullable: true),
                    Risk = table.Column<int>(type: "INTEGER", nullable: true),
                    SharpeRatio = table.Column<decimal>(type: "REAL", nullable: true),
                    StandardDeviation = table.Column<decimal>(type: "REAL", nullable: true),
                    SustainabilityLevel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SustainabilityRating = table.Column<int>(type: "INTEGER", nullable: true),
                    EsgScore = table.Column<decimal>(type: "REAL", nullable: true),
                    EnvironmentalScore = table.Column<decimal>(type: "REAL", nullable: true),
                    SocialScore = table.Column<decimal>(type: "REAL", nullable: true),
                    GovernanceScore = table.Column<decimal>(type: "REAL", nullable: true),
                    LowCarbon = table.Column<bool>(type: "INTEGER", nullable: true),
                    EuArticleType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CrawlerLastUpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AboutFundLastVisitedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundProfiles", x => x.Isin);
                });

            migrationBuilder.CreateTable(
                name: "FundHistoryRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    FundId = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 12, nullable: false),
                    Nav = table.Column<decimal>(type: "REAL", nullable: true),
                    NavDate = table.Column<DateOnly>(type: "TEXT", maxLength: 10, nullable: true),
                    Capital = table.Column<decimal>(type: "REAL", nullable: true),
                    NumberOfOwners = table.Column<int>(type: "INTEGER", nullable: true),
                    Risk = table.Column<int>(type: "INTEGER", nullable: true),
                    SharpeRatio = table.Column<decimal>(type: "REAL", nullable: true),
                    StandardDeviation = table.Column<decimal>(type: "REAL", nullable: true)
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
                descending: new[] { false, true });
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

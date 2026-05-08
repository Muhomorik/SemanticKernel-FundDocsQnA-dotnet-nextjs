using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Infrastructure.FundData.Migrations
{
    /// <inheritdoc />
    public partial class AddFundAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.CountryId);
                });

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    SectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.SectorId);
                });

            migrationBuilder.CreateTable(
                name: "FundCountryAllocations",
                columns: table => new
                {
                    FundCountryAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Isin = table.Column<string>(type: "NCHAR(12)", fixedLength: true, nullable: false),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Percentage = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundCountryAllocations", x => x.FundCountryAllocationId);
                    table.ForeignKey(
                        name: "FK_FundCountryAllocations_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FundCountryAllocations_FundProfiles_Isin",
                        column: x => x.Isin,
                        principalTable: "FundProfiles",
                        principalColumn: "Isin",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundSectorAllocations",
                columns: table => new
                {
                    FundSectorAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Isin = table.Column<string>(type: "NCHAR(12)", fixedLength: true, nullable: false),
                    SectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Percentage = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundSectorAllocations", x => x.FundSectorAllocationId);
                    table.ForeignKey(
                        name: "FK_FundSectorAllocations_FundProfiles_Isin",
                        column: x => x.Isin,
                        principalTable: "FundProfiles",
                        principalColumn: "Isin",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundSectorAllocations_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "SectorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Countries_DisplayName",
                table: "Countries",
                column: "DisplayName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundCountryAllocations_CountryId",
                table: "FundCountryAllocations",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "UX_FundCountryAllocations_Isin_CountryId",
                table: "FundCountryAllocations",
                columns: new[] { "Isin", "CountryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundSectorAllocations_SectorId",
                table: "FundSectorAllocations",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "UX_FundSectorAllocations_Isin_SectorId",
                table: "FundSectorAllocations",
                columns: new[] { "Isin", "SectorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Sectors_DisplayName",
                table: "Sectors",
                column: "DisplayName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundCountryAllocations");

            migrationBuilder.DropTable(
                name: "FundSectorAllocations");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Sectors");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.API.Infrastructure.FundData.Migrations
{
    /// <inheritdoc />
    public partial class AddFundProfileDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FundProfiles",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "FundProfiles");
        }
    }
}

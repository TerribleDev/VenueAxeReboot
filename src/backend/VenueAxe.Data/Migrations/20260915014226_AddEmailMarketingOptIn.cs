using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenueAxe.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailMarketingOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailMarketingOptIn",
                table: "waivers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EmailMarketingOptIn",
                table: "bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailMarketingOptIn",
                table: "waivers");

            migrationBuilder.DropColumn(
                name: "EmailMarketingOptIn",
                table: "bookings");
        }
    }
}

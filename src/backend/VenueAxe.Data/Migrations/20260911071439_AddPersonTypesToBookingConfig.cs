using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenueAxe.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonTypesToBookingConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersonBreakdownJson",
                table: "bookings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonTypesJson",
                table: "booking_configs",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonBreakdownJson",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PersonTypesJson",
                table: "booking_configs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenueAxe.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliedDiscountCode",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingTypeId",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountAmountCents",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SquareOrderId",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SquarePaymentId",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddonsJson",
                table: "booking_configs",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BookingTypesJson",
                table: "booking_configs",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DiscountRulesJson",
                table: "booking_configs",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedDiscountCode",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "BookingTypeId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "DiscountAmountCents",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "SquareOrderId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "SquarePaymentId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "AddonsJson",
                table: "booking_configs");

            migrationBuilder.DropColumn(
                name: "BookingTypesJson",
                table: "booking_configs");

            migrationBuilder.DropColumn(
                name: "DiscountRulesJson",
                table: "booking_configs");
        }
    }
}

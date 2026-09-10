using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenueAxe.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetCellIndexToMatchThrow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetCellIndex",
                table: "match_throws",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetCellIndex",
                table: "match_throws");
        }
    }
}

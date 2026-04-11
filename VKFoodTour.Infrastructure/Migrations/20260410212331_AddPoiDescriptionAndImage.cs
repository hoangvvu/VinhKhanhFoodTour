using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VKFoodTour.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiDescriptionAndImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "POIS",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "POIS",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "POIS");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "POIS");
        }
    }
}

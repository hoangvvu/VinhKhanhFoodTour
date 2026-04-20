using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VKFoodTour.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rejection_note",
                table: "POIS",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "POIS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "APP_FEEDBACK",
                columns: table => new
                {
                    feedback_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    device_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    rating = table.Column<byte>(type: "tinyint", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    app_version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_FEEDBACK", x => x.feedback_id);
                });

            migrationBuilder.CreateTable(
                name: "TOUR_SETTINGS",
                columns: table => new
                {
                    setting_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    setting_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    setting_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOUR_SETTINGS", x => x.setting_id);
                });

            migrationBuilder.CreateIndex(
                name: "uq_tour_setting_key",
                table: "TOUR_SETTINGS",
                column: "setting_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APP_FEEDBACK");

            migrationBuilder.DropTable(
                name: "TOUR_SETTINGS");

            migrationBuilder.DropColumn(
                name: "rejection_note",
                table: "POIS");

            migrationBuilder.DropColumn(
                name: "status",
                table: "POIS");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VKFoodTour.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioUrlToNarrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IMAGES_POIS_poi_id",
                table: "IMAGES");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "MENU_ITEMS",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IMAGES_POIS_poi_id",
                table: "IMAGES",
                column: "poi_id",
                principalTable: "POIS",
                principalColumn: "poi_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IMAGES_POIS_poi_id",
                table: "IMAGES");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "MENU_ITEMS",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddForeignKey(
                name: "FK_IMAGES_POIS_poi_id",
                table: "IMAGES",
                column: "poi_id",
                principalTable: "POIS",
                principalColumn: "poi_id");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VKFoodTour.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "membership_tier",
                table: "USERS",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "membership_tier",
                table: "USERS");
        }
    }
}

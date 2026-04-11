using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VKFoodTour.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LANGUAGES",
                columns: table => new
                {
                    language_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    tts_voice = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LANGUAGES", x => x.language_id);
                });

            migrationBuilder.CreateTable(
                name: "MENU_ITEMS",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    poi_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    audio_url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MENU_ITEMS", x => x.item_id);
                });

            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "POIS",
                columns: table => new
                {
                    poi_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    owner_id = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: false),
                    longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    radius = table.Column<int>(type: "int", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POIS", x => x.poi_id);
                    table.ForeignKey(
                        name: "FK_POIS_USERS_owner_id",
                        column: x => x.owner_id,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FOODS",
                columns: table => new
                {
                    food_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    poi_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    is_available = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FOODS", x => x.food_id);
                    table.ForeignKey(
                        name: "FK_FOODS_POIS_poi_id",
                        column: x => x.poi_id,
                        principalTable: "POIS",
                        principalColumn: "poi_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NARRATIONS",
                columns: table => new
                {
                    narration_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    poi_id = table.Column<int>(type: "int", nullable: false),
                    language_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tts_voice = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NARRATIONS", x => x.narration_id);
                    table.ForeignKey(
                        name: "FK_NARRATIONS_LANGUAGES_language_id",
                        column: x => x.language_id,
                        principalTable: "LANGUAGES",
                        principalColumn: "language_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NARRATIONS_POIS_poi_id",
                        column: x => x.poi_id,
                        principalTable: "POIS",
                        principalColumn: "poi_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QRCODES",
                columns: table => new
                {
                    qr_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    poi_id = table.Column<int>(type: "int", nullable: false),
                    qr_token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    location_note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRCODES", x => x.qr_id);
                    table.ForeignKey(
                        name: "FK_QRCODES_POIS_poi_id",
                        column: x => x.poi_id,
                        principalTable: "POIS",
                        principalColumn: "poi_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "REVIEWS",
                columns: table => new
                {
                    review_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    device_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    poi_id = table.Column<int>(type: "int", nullable: false),
                    rating = table.Column<byte>(type: "tinyint", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    language_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REVIEWS", x => x.review_id);
                    table.ForeignKey(
                        name: "FK_REVIEWS_POIS_poi_id",
                        column: x => x.poi_id,
                        principalTable: "POIS",
                        principalColumn: "poi_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TRACKING_LOGS",
                columns: table => new
                {
                    log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    device_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    poi_id = table.Column<int>(type: "int", nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: false),
                    longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    event_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    listened_duration_sec = table.Column<int>(type: "int", nullable: true),
                    language_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRACKING_LOGS", x => x.log_id);
                    table.ForeignKey(
                        name: "FK_TRACKING_LOGS_POIS_poi_id",
                        column: x => x.poi_id,
                        principalTable: "POIS",
                        principalColumn: "poi_id");
                });

            migrationBuilder.CreateTable(
                name: "FOOD_TRANSLATIONS",
                columns: table => new
                {
                    translation_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    food_id = table.Column<int>(type: "int", nullable: false),
                    language_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FOOD_TRANSLATIONS", x => x.translation_id);
                    table.ForeignKey(
                        name: "FK_FOOD_TRANSLATIONS_FOODS_food_id",
                        column: x => x.food_id,
                        principalTable: "FOODS",
                        principalColumn: "food_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FOOD_TRANSLATIONS_LANGUAGES_language_id",
                        column: x => x.language_id,
                        principalTable: "LANGUAGES",
                        principalColumn: "language_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IMAGES",
                columns: table => new
                {
                    image_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    poi_id = table.Column<int>(type: "int", nullable: true),
                    food_id = table.Column<int>(type: "int", nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    alt_text = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    is_cover = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IMAGES", x => x.image_id);
                    table.ForeignKey(
                        name: "FK_IMAGES_FOODS_food_id",
                        column: x => x.food_id,
                        principalTable: "FOODS",
                        principalColumn: "food_id");
                    table.ForeignKey(
                        name: "FK_IMAGES_POIS_poi_id",
                        column: x => x.poi_id,
                        principalTable: "POIS",
                        principalColumn: "poi_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FOOD_TRANSLATIONS_language_id",
                table: "FOOD_TRANSLATIONS",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "uq_food_translation",
                table: "FOOD_TRANSLATIONS",
                columns: new[] { "food_id", "language_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FOODS_poi_id",
                table: "FOODS",
                column: "poi_id");

            migrationBuilder.CreateIndex(
                name: "IX_IMAGES_food_id",
                table: "IMAGES",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "IX_IMAGES_poi_id",
                table: "IMAGES",
                column: "poi_id");

            migrationBuilder.CreateIndex(
                name: "IX_LANGUAGES_code",
                table: "LANGUAGES",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_narrations_poi_lang",
                table: "NARRATIONS",
                columns: new[] { "poi_id", "language_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NARRATIONS_language_id",
                table: "NARRATIONS",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "idx_pois_geo",
                table: "POIS",
                columns: new[] { "latitude", "longitude" },
                filter: "[is_active] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_POIS_owner_id",
                table: "POIS",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "idx_qr_token",
                table: "QRCODES",
                column: "qr_token",
                unique: true,
                filter: "[is_active] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_QRCODES_poi_id",
                table: "QRCODES",
                column: "poi_id");

            migrationBuilder.CreateIndex(
                name: "IX_REVIEWS_poi_id",
                table: "REVIEWS",
                column: "poi_id");

            migrationBuilder.CreateIndex(
                name: "idx_tracking_device",
                table: "TRACKING_LOGS",
                columns: new[] { "device_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_tracking_geo",
                table: "TRACKING_LOGS",
                columns: new[] { "latitude", "longitude" });

            migrationBuilder.CreateIndex(
                name: "idx_tracking_poi",
                table: "TRACKING_LOGS",
                columns: new[] { "poi_id", "event_type", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_USERS_email",
                table: "USERS",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FOOD_TRANSLATIONS");

            migrationBuilder.DropTable(
                name: "IMAGES");

            migrationBuilder.DropTable(
                name: "MENU_ITEMS");

            migrationBuilder.DropTable(
                name: "NARRATIONS");

            migrationBuilder.DropTable(
                name: "QRCODES");

            migrationBuilder.DropTable(
                name: "REVIEWS");

            migrationBuilder.DropTable(
                name: "TRACKING_LOGS");

            migrationBuilder.DropTable(
                name: "FOODS");

            migrationBuilder.DropTable(
                name: "LANGUAGES");

            migrationBuilder.DropTable(
                name: "POIS");

            migrationBuilder.DropTable(
                name: "USERS");
        }
    }
}

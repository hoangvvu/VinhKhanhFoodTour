using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using VinhKhanhFoodTour.AdminWeb.Data;

namespace VinhKhanhFoodTour.AdminWeb.Migrations
{
    [DbContext(typeof(AdminDbContext))]
    partial class AdminDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("VinhKhanhFoodTour.AdminWeb.Models.AdminUser", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLogin")
                        .HasColumnType("datetime2");

                    b.Property<int?>("ManagedShopId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("UserId");

                    b.HasIndex("ManagedShopId");

                    b.ToTable("AdminUsers");
                });

            modelBuilder.Entity("VinhKhanhFoodTour.AdminWeb.Models.AuditLog", b =>
                {
                    b.Property<int>("AuditId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("AuditId"));

                    b.Property<string>("Action")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int?>("EntityId")
                        .HasColumnType("int");

                    b.Property<string>("EntityType")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("NewValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("AuditId");

                    b.ToTable("AuditLogs");
                });

            modelBuilder.Entity("VinhKhanhFoodTour.AdminWeb.Models.ManagedShop", b =>
                {
                    b.Property<int>("ShopId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ShopId"));

                    b.Property<string>("Address")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<decimal>("AverageRating")
                        .HasPrecision(3, 2)
                        .HasColumnType("decimal(3,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("bit");

                    b.Property<decimal?>("Latitude")
                        .HasColumnType("decimal(18,6)");

                    b.Property<decimal?>("Longitude")
                        .HasColumnType("decimal(18,6)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int?>("OwnerId")
                        .HasColumnType("int");

                    b.Property<int>("Radius")
                        .HasColumnType("int");

                    b.Property<int>("TotalOrders")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("ShopId");

                    b.ToTable("ManagedShops");
                });

            modelBuilder.Entity("VinhKhanhFoodTour.AdminWeb.Models.ShopStatistics", b =>
                {
                    b.Property<int>("ShopId")
                        .HasColumnType("int");

                    b.Property<DateTime>("StatisticsDate")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("AverageRating")
                        .HasPrecision(3, 2)
                        .HasColumnType("decimal(3,2)");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("ShopName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("TotalOrders")
                        .HasColumnType("int");

                    b.Property<int>("TotalVisits")
                        .HasColumnType("int");

                    b.Property<decimal>("TotalRevenue")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("ShopId", "StatisticsDate");

                    b.ToTable("ShopStatistics");
                });

            modelBuilder.Entity("VinhKhanhFoodTour.AdminWeb.Models.AdminUser", b =>
                {
                    b.HasOne("VinhKhanhFoodTour.AdminWeb.Models.ManagedShop", "ManagedShop")
                        .WithMany("Managers")
                        .HasForeignKey("ManagedShopId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("ManagedShop");
                });

            modelBuilder.Entity("VinhKhanhFoodTour.AdminWeb.Models.ManagedShop", b =>
                {
                    b.Navigation("Managers");
                });
#pragma warning restore 612, 618
        }
    }
}

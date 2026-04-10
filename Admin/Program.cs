using Admin.Components;
using Microsoft.Extensions.FileProviders;
using Admin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// --- ĐĂNG KÝ SERVICES ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 15 * 1024 * 1024; // 15MB
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.Name = "VK_Admin_Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpClient();

builder.Services.AddScoped<PoiService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TtsService>();

var app = builder.Build();

// --- KHỞI TẠO DỮ LIỆU ---
// --- KHỞI TẠO DỮ LIỆU (ĐÃ ĐƯỢC BẢO VỆ CHỐNG SẬP) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        // Kiểm tra xem có kết nối được SQL Server không
        bool canConnect = await db.Database.CanConnectAsync();
        if (canConnect)
        {
            await SeedData.InitializeAsync(db);
        }
        else
        {
            Console.WriteLine("\n=======================================================");
            Console.WriteLine("🚨 CẢNH BÁO: KHÔNG THỂ KẾT NỐI TỚI DATABASE SQL SERVER!");
            Console.WriteLine("👉 Hãy kiểm tra lại SQL Server đã bật chưa và xem file appsettings.json");
            Console.WriteLine("=======================================================\n");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("\n=======================================================");
        Console.WriteLine($"🚨 LỖI TẠO DỮ LIỆU MẪU (Bảng USERS có thể chưa tồn tại):");
        Console.WriteLine(ex.Message);
        Console.WriteLine("=======================================================\n");
    }
}

// 1. Phục vụ file tĩnh trong wwwroot (CSS, JS)
app.UseStaticFiles();

// 2. ĐÃ SỬA: Đẩy thư mục UploadsData ra HẲN BÊN NGOÀI project (lùi 1 cấp "..")
var uploadsFolderPath = Path.Combine(builder.Environment.ContentRootPath, "..", "UploadsData");
if (!Directory.Exists(uploadsFolderPath))
{
    Directory.CreateDirectory(Path.Combine(uploadsFolderPath, "poi"));
    Directory.CreateDirectory(Path.Combine(uploadsFolderPath, "menu"));
}

// Mở cổng truy cập ảnh upload qua đường dẫn /uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsFolderPath),
    RequestPath = "/uploads"
});

// 3. THỨ TỰ BẮT BUỘC: Routing -> Auth -> Antiforgery -> Map
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
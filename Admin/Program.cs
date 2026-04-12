using Admin.Components;
using Microsoft.Extensions.FileProviders;
using Admin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ SERVICES ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 15 * 1024 * 1024; // Hỗ trợ upload 15MB
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

// Đăng ký các Service (Dựa theo các file Service.cs bạn vừa gửi)
builder.Services.AddScoped<PoiService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TtsService>();

var app = builder.Build();

// --- 2. KHỞI TẠO DỮ LIỆU (SeedData) ---
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await SeedData.InitializeAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LỖI KHỞI TẠO]: {ex.Message}");
    }
}

// --- 3. CẤU HÌNH PIPELINE ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// QUAN TRỌNG: Thứ tự các StaticFiles
app.UseStaticFiles(); // Cho wwwroot

// Cấu hình thư mục UploadsData (Giấu khỏi Hot Reload để tránh sập Terminal)
var uploadsPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "UploadsData"));
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(Path.Combine(uploadsPath, "poi"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "menu"));
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Không dùng MapStaticAssets ở đây để tránh lỗi manifest khi thêm file lúc runtime
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
using Admin.Components;
using Admin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VKFoodTour.Infrastructure.Data;  // ★ Dùng Infrastructure DbContext

var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ SERVICES (DI CONTAINER) ---

// Hỗ trợ giao diện Blazor và Tương tác Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

// ★ Đăng ký Infrastructure DbContext (thay thế Admin.Data.ApplicationDbContext)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cần HttpContextAccessor để đọc Cookie Auth trong Blazor Server
builder.Services.AddHttpContextAccessor();

// Cấu hình Xác thực bằng Cookie (Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.Name = "VK_Admin_Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Kích hoạt Phân quyền (Authorization)
builder.Services.AddAuthorization();

builder.Services.AddScoped<MenuService>();

// Quan trọng: Giúp Blazor nhận diện trạng thái Đăng nhập trên toàn App
builder.Services.AddCascadingAuthenticationState();

// Đăng ký HttpClient cho các Services
builder.Services.AddHttpClient();

// Đăng ký các Service nghiệp vụ
builder.Services.AddScoped<PoiService>();
builder.Services.AddScoped<AuthService>();  // ★ MỚI: Service đăng nhập
builder.Services.AddScoped<TtsService>();   // ★ MỚI: Service Text-to-Speech

var app = builder.Build();

// --- 2. SEED DỮ LIỆU ADMIN MẶC ĐỊNH ---
// Tạo tài khoản Admin đầu tiên nếu DB chưa có user nào
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SeedData.InitializeAsync(db);
}

// --- 3. CẤU HÌNH HTTP PIPELINE (MIDDLEWARE) ---
app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
// QUAN TRỌNG: Thứ tự các dòng dưới đây không được thay đổi
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
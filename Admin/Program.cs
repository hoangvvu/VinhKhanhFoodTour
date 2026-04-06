using Admin.Components;
using Admin.Data;
using Admin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ SERVICES (DI CONTAINER) ---

// Hỗ trợ giao diện Blazor và Tương tác Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Đăng ký Database Context (Kết nối SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Xác thực bằng Cookie (Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login"; // Trang chuyển hướng nếu chưa đăng nhập
        options.AccessDeniedPath = "/access-denied"; // Trang nếu sai Role
        options.Cookie.Name = "VK_Admin_Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Hết hạn sau 8 tiếng
    });

// Kích hoạt Phân quyền (Authorization)
builder.Services.AddAuthorization();

// Quan trọng: Giúp Blazor nhận diện trạng thái Đăng nhập trên toàn App
builder.Services.AddCascadingAuthenticationState();

// Đăng ký các Service nghiệp vụ của bạn
builder.Services.AddScoped<PoiService>();
// builder.Services.AddScoped<UserService>(); // Nếu bạn có service quản lý user

var app = builder.Build();

// --- 2. CẤU HÌNH HTTP PIPELINE (MIDDLEWARE) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// QUAN TRỌNG: Thứ tự các dòng dưới đây không được thay đổi
app.MapStaticAssets(); // Xử lý file tĩnh (CSS/JS)

app.UseAuthentication(); // 1. Kiểm tra bạn là ai?
app.UseAuthorization();  // 2. Bạn có quyền làm gì?

app.UseAntiforgery();    // 3. Chống tấn công giả mạo

// Đăng ký các Component và Render Mode
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using VKFoodTour.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// 1. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES) VÀO DEPENDENCY INJECTION CONTAINER
// ==============================================================================

// a. Đăng ký ApplicationDbContext với chuỗi kết nối từ appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// b. Cấu hình CORS (Rất quan trọng: Cho phép App Mobile và Web CMS gọi được API này)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // Cho phép mọi domain gọi tới
              .AllowAnyHeader()   // Cho phép mọi Header
              .AllowAnyMethod();  // Cho phép mọi method (GET, POST, PUT, DELETE)
    });
});

// c. Đăng ký Controllers (Xử lý các HTTP Request)
builder.Services.AddControllers();

// d. Cấu hình Swagger (Giao diện tự động sinh tài liệu API để bạn dễ dàng test)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// (Sau này bạn sẽ đăng ký thêm các Services/Repositories khác ở đây)
// builder.Services.AddScoped<IPoiService, PoiService>();
// builder.Services.AddScoped<IAuthService, AuthService>();

// ==============================================================================
// 2. BUILD APP VÀ CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE)
// ==============================================================================
var app = builder.Build();
// Giữ DB theo script SQL thủ công: không auto chạy EF migration khi khởi động API.

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH('NARRATIONS', 'audio_url') IS NULL
    ALTER TABLE NARRATIONS ADD audio_url NVARCHAR(2048) NULL;
ELSE
    ALTER TABLE NARRATIONS ALTER COLUMN audio_url NVARCHAR(2048) NULL;

IF COL_LENGTH('NARRATIONS', 'audio_url_auto') IS NULL
    ALTER TABLE NARRATIONS ADD audio_url_auto NVARCHAR(2048) NULL;
ELSE
    ALTER TABLE NARRATIONS ALTER COLUMN audio_url_auto NVARCHAR(2048) NULL;

IF COL_LENGTH('NARRATIONS', 'audio_url_qr') IS NULL
    ALTER TABLE NARRATIONS ADD audio_url_qr NVARCHAR(2048) NULL;
ELSE
    ALTER TABLE NARRATIONS ALTER COLUMN audio_url_qr NVARCHAR(2048) NULL;
""");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB SCHEMA CHECK] {ex.Message}");
    }
}

if (app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    // Mặc định mở cổng LAN để thiết bị Android thật truy cập được API.
    app.Urls.Add("http://0.0.0.0:5242");
}

// a. Kích hoạt Swagger khi đang chạy ở chế độ Development (Local)
// Tạm thời bỏ kiểm tra môi trường để ép Swagger luôn chạy
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VK Food Tour API v1");
});

// b. HTTPS redirect: tắt khi Development để app mobile (HTTP tới 10.0.2.2 / LAN) không bị chuyển sang cổng HTTPS khác
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// c. Áp dụng chính sách CORS đã cấu hình ở trên
app.UseCors("AllowAll");

// c2. Ảnh / audio upload (cùng thư mục với Admin) — mobile ghép ApiBaseUrl + /uploads/...
var uploadsPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "UploadsData"));
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(Path.Combine(uploadsPath, "poi"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "menu"));
}

Directory.CreateDirectory(Path.Combine(uploadsPath, "narration"));

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// d. Xác thực (Authentication - Đăng nhập) và Phân quyền (Authorization)
// app.UseAuthentication(); // (Sẽ mở comment dòng này khi bạn cấu hình JWT Token)
app.UseAuthorization();

// e. Map các đường dẫn API tới các Controllers tương ứng
app.MapControllers();

// Chạy ứng dụng
app.Run();
using Microsoft.EntityFrameworkCore;
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

// d. Xác thực (Authentication - Đăng nhập) và Phân quyền (Authorization)
// app.UseAuthentication(); // (Sẽ mở comment dòng này khi bạn cấu hình JWT Token)
app.UseAuthorization();

// e. Map các đường dẫn API tới các Controllers tương ứng
app.MapControllers();

// Chạy ứng dụng
app.Run();
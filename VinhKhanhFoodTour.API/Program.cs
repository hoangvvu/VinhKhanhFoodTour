using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.API.Models; // Đảm bảo đúng tên thư mục Models của bạn

var builder = WebApplication.CreateBuilder(args);

// 1. Bật tính năng đọc Controller (Cái này rất hay bị thiếu)
builder.Services.AddControllers();

// 2. Khai báo Database
builder.Services.AddDbContext<VinhkhanhFoodtourContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Bật tính năng Swagger (Giao diện web để test)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Cấu hình hiển thị giao diện Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// 5. Kích hoạt đường dẫn (Route) cho Controller
app.MapControllers();

app.Run();
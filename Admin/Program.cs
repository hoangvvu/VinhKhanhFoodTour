using Admin.Components;
using Admin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Data;
using System.Security.Claims;
using VKFoodTour.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 15 * 1024 * 1024;
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Factory cho các Blazor component dùng Timer (InteractiveServer) — tránh DbContext disposed error
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")),
    ServiceLifetime.Scoped);

builder.Services.AddHttpContextAccessor();

var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.Name = "VK_Admin_Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.Events.OnTicketReceived = async ctx =>
        {
            var email = ctx.Principal?.FindFirstValue(ClaimTypes.Email)
                ?? ctx.Principal?.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
            if (string.IsNullOrEmpty(email))
            {
                ctx.HandleResponse();
                ctx.Response.Redirect("/login?error=noemail");
                return;
            }

            var displayName = ctx.Principal?.FindFirstValue(ClaimTypes.Name)
                ?? ctx.Principal?.FindFirstValue("given_name");

            using var scope = ctx.HttpContext.RequestServices.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
            VKFoodTour.Infrastructure.Entities.User user;
            try
            {
                user = await authService.FindOrCreateUserFromGoogleAsync(email, displayName);
            }
            catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_DISABLED")
            {
                ctx.HandleResponse();
                ctx.Response.Redirect("/login?error=disabled");
                return;
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await ctx.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            var redirect = ctx.Properties?.RedirectUri;
            if (string.IsNullOrEmpty(redirect))
                redirect = "/";

            ctx.HandleResponse();
            ctx.Response.Redirect(redirect);
        };
    });
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddTransient<TtsService>();
builder.Services.AddTransient<EdgeTtsService>();
builder.Services.AddHttpClient<GoogleTranslateService>();

builder.Services.AddScoped<PoiService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await ShouldRunMigrationsAsync(db))
            await db.Database.MigrateAsync();
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
        await SeedData.InitializeAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LỖI CSDL / SEED]: {ex.Message}");
    }
}

static async Task<bool> ShouldRunMigrationsAsync(ApplicationDbContext db)
{
    // DB tạo bằng script thủ công thường đã có bảng nghiệp vụ nhưng không có __EFMigrationsHistory.
    // Trường hợp đó bỏ qua Migrate để tránh lỗi "object already exists".
    await using var conn = db.Database.GetDbConnection();
    if (conn.State != ConnectionState.Open)
        await conn.OpenAsync();

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
SELECT
    CASE WHEN OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL THEN 0 ELSE 1 END AS HasHistory,
    CASE WHEN OBJECT_ID(N'dbo.LANGUAGES', N'U') IS NULL THEN 0 ELSE 1 END AS HasLanguages
""";

    await using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return true;

    var hasHistory = reader.GetInt32(0) == 1;
    var hasLanguages = reader.GetInt32(1) == 1;

    if (!hasHistory && hasLanguages)
    {
        Console.WriteLine("[DB INIT] Skip EF migration: schema has been created manually.");
        return false;
    }

    return true;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

var uploadsPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "UploadsData"));
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(Path.Combine(uploadsPath, "poi"));
    Directory.CreateDirectory(Path.Combine(uploadsPath, "menu"));
}

Directory.CreateDirectory(Path.Combine(uploadsPath, "narration"));

var contentTypes = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypes.Mappings[".mp3"] = "audio/mpeg";
contentTypes.Mappings[".wav"] = "audio/wav";
contentTypes.Mappings[".m4a"] = "audio/mp4";
contentTypes.Mappings[".ogg"] = "audio/ogg";
contentTypes.Mappings[".aac"] = "audio/aac";
contentTypes.Mappings[".heic"] = "image/heic";
contentTypes.Mappings[".webp"] = "image/webp";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ContentTypeProvider = contentTypes,
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    app.MapGet("/auth/google", (HttpContext http) =>
        http.ChallengeAsync(
            GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = "/" })).AllowAnonymous();
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

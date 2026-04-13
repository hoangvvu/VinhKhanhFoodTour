using Admin.Components;
using Admin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
            var user = await authService.FindOrCreateUserFromGoogleAsync(email, displayName);

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
builder.Services.AddHttpClient<TtsService>();
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
        await db.Database.MigrateAsync();
        await SeedData.InitializeAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LỖI CSDL / SEED]: {ex.Message}");
    }
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

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
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

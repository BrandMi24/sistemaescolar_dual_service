using ControlEscolar.Data;
using ControlEscolar.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Console.WriteLine("=== EXCEPCION NO MANEJADA ===");
    Console.WriteLine(e.ExceptionObject.ToString());
    Console.WriteLine("============================");
};

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ControlEscolar.Services.IFileService, ControlEscolar.Services.FileService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Configurar Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Límite de tamaño de archivos subidos (5 MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5_242_880; // 5 MB
});

// Configuracion de cookies para autenticación
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300, // Aumentado de 30 a 300
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Cabeceras de seguridad
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://code.jquery.com https://cdn.datatables.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://cdn.datatables.net; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data:; " +
        "connect-src 'self' https://cdn.jsdelivr.net https://cdn.datatables.net ws: wss: http://localhost:* https://localhost:*;");
    await next();
});

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Migración automática y seed del admin (solo en desarrollo)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        //if (app.Environment.IsDevelopment())
        //{
        //    context.Database.Migrate();
        //}
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al inicializar la base de datos");
    }
}

app.Run();

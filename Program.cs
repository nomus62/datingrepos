using System.Text;

using DatingApp.Server.Data;
using DatingApp.Server.Services;
using DatingApp.Server.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ===== ЯВНАЯ ЗАГРУЗКА КОНФИГУРАЦИИ =====
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// Порт от Render (или по умолчанию 8080)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// === ПРОВЕРКА СТРОКИ ПОДКЛЮЧЕНИЯ ===
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connString))
{
    Console.WriteLine("FATAL: Connection string 'DefaultConnection' is not configured.");
    throw new InvalidOperationException("Connection string is missing.");
}

// === JWT НАСТРОЙКИ ===
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    Console.WriteLine("FATAL: JWT settings are not configured.");
    throw new InvalidOperationException("JWT settings are missing.");
}
var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

// === РЕГИСТРАЦИЯ СЕРВИСОВ ===
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// === DB CONTEXT (ТОЛЬКО POSTGRESQL) ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connString));

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();

var app = builder.Build();

// === МИГРАЦИИ (с обработкой ошибок) ===
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Migrations applied successfully.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Migration failed: {ex.Message}");
    // Приложение продолжит работу, но БД может быть недоступна
}

// === ПОРЯДОК MIDDLEWARE ===
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles();

app.Run();
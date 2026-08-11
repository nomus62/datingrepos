using System.Text;
using DatingApp.Server.Data;
using DatingApp.Server.Services;
using DatingApp.Server.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Настройка конфигурации
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Принудительно используем порт от Render (если не задан, то 8080)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Проверяем наличие строки подключения (если нет — выводим ошибку, но не падаем)
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connString))
{
    Console.WriteLine("WARNING: Connection string 'DefaultConnection' is not configured. Database features may not work.");
}

// Настройка JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    Console.WriteLine("WARNING: JWT settings are not configured properly. Authentication may not work.");
}

if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
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
}

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

// Регистрируем DbContext только если есть строка подключения
if (!string.IsNullOrEmpty(connString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connString));
}
else
{
    // Временно используем заглушку, чтобы приложение запустилось
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("DummyDb"));
}

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddControllers();

var app = builder.Build();

// Применяем миграции, только если строка подключения существует
if (!string.IsNullOrEmpty(connString))
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("Database migrations applied successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex.Message}");
    }
}

app.UseCors("AllowAll");

if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();
app.UseStaticFiles();

app.Run();
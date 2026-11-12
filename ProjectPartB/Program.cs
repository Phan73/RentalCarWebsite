using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectPartB.Areas.Identity.Data;
using ProjectPartB.Data;
using ProjectPartB.Models;
using ProjectPartB.Services;

// Support DateTime binding with Npgsql PostgreSQL timestamps
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Cross-Instance Data Protection Key Persistence (Eliminates login dropouts across container restarts & multi-instance scaling)
var keysDir = Path.Combine(Path.GetTempPath(), "rentalcar_dp_keys");
if (!Directory.Exists(keysDir))
{
    Directory.CreateDirectory(keysDir);
}
var primaryKeyPath = Path.Combine(keysDir, "key-6cac850f-ac76-40ea-8c02-23f5afa16ed7.xml");
if (!File.Exists(primaryKeyPath))
{
    const string masterKeyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<key id=""6cac850f-ac76-40ea-8c02-23f5afa16ed7"" version=""1"">
  <creationDate>2026-11-11T00:00:00.0000000Z</creationDate>
  <activationDate>2026-11-11T00:00:00.0000000Z</activationDate>
  <expirationDate>2036-12-31T23:59:59.0000000Z</expirationDate>
  <descriptor deserializerType=""Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60"">
    <descriptor>
      <encryption algorithm=""AES_256_CBC"" />
      <validation algorithm=""HMACSHA256"" />
      <masterKey p4:requiresEncryption=""true"" xmlns:p4=""http://schemas.asp.net/2015/03/dataProtection"">
        <value>nDbEuP9p0oKbcyZIKmCVdfVHFqh26EE0SOHdoniLTCQjM2KQeIUDyUjSN8jiNbIYTCn0v6bKfWNmWu9WYIQKkg==</value>
      </masterKey>
    </descriptor>
  </descriptor>
</key>";
    File.WriteAllText(primaryKeyPath, masterKeyXml);
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
    .SetApplicationName("RentalCarWebsite");

// Support dynamic port injection (e.g. Vercel Container runtime $PORT)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// Configuration & Connection Strings
var configuration = builder.Configuration;
var defaultConn = configuration.GetConnectionString("DefaultConnection");
var postgresConn = ResolvePostgresConnectionString(configuration);

if (!string.IsNullOrEmpty(postgresConn))
{
    Console.WriteLine("[Database] Configured for PostgreSQL (Cloud Database).");
    builder.Services.AddDbContext<CustomIdentityContext>(options =>
        options.UseNpgsql(postgresConn, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));
    builder.Services.AddDbContext<FS23_Group4_ProjectContext>(options =>
        options.UseNpgsql(postgresConn, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));
}
else
{
    bool useSqlite = configuration.GetValue<bool>("UseSqlite") ||
                     (defaultConn != null && defaultConn.Contains(".db", StringComparison.OrdinalIgnoreCase)) ||
                     string.IsNullOrEmpty(defaultConn);

    if (useSqlite)
    {
        Console.WriteLine("[Database] Configured for SQLite.");
        var sqliteConn = configuration.GetConnectionString("SqliteConnection") 
                         ?? (defaultConn != null && defaultConn.Contains(".db", StringComparison.OrdinalIgnoreCase) ? defaultConn : null)
                         ?? "Data Source=RentalCarWebsite.db";
        builder.Services.AddDbContext<CustomIdentityContext>(options =>
            options.UseSqlite(sqliteConn));
        builder.Services.AddDbContext<FS23_Group4_ProjectContext>(options =>
            options.UseSqlite(sqliteConn));
    }
    else
    {
        Console.WriteLine("[Database] Configured for SQL Server.");
        builder.Services.AddDbContext<CustomIdentityContext>(options =>
            options.UseSqlServer(defaultConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));
        builder.Services.AddDbContext<FS23_Group4_ProjectContext>(options =>
            options.UseSqlServer(defaultConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));
    }
}

// ASP.NET Core Identity
builder.Services.AddDefaultIdentity<WebUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Accessible self-registration
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<CustomIdentityContext>();

// Application & Business Services
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IFleetService, FleetService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<IEmailSender, EmailNotificationService>();

// MVC & Razor
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Optional External Google Authentication
var googleClientId = configuration["Authentication:Google:ClientId"];
var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = googleClientId;
        googleOptions.ClientSecret = googleClientSecret;
    });
}

// Session Configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(45);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// Application Authentication Cookie Configuration (Persistent Login & Remember Me)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".ApexRentalCar.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromHours(24);
});

// Antiforgery Cookie Configuration
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".ApexRentalCar.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Forwarded Headers for Cloud/Reverse Proxy Environments (Vercel, Docker)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Role-Based Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequireStaff", policy => policy.RequireRole("Administrator", "Manager", "Staff"));
    options.AddPolicy("RequireCustomer", policy => policy.RequireRole("Administrator", "Manager", "Staff", "Customer", "Visitor"));
});

var app = builder.Build();

// Seed Database (Roles, Locations, Categories, Vehicles, Initial Data)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var projectCtx = services.GetRequiredService<FS23_Group4_ProjectContext>();
        var identityCtx = services.GetRequiredService<CustomIdentityContext>();
        var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = services.GetRequiredService<UserManager<WebUser>>();

        await DatabaseSeeder.SeedAsync(projectCtx, identityCtx, roleMgr, userMgr);
        logger.LogInformation("Database initialized and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing and seeding the database.");
    }
}

// HTTP Request Pipeline
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

static string? ResolvePostgresConnectionString(IConfiguration configuration)
{
    var rawUrl = configuration.GetConnectionString("PostgreSqlConnection")
                 ?? configuration.GetConnectionString("PostgresConnection")
                 ?? Environment.GetEnvironmentVariable("POSTGRES_URL")
                 ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                 ?? Environment.GetEnvironmentVariable("POSTGRES_PRISMA_URL");

    if (!string.IsNullOrEmpty(rawUrl))
    {
        return FormatNpgsqlConnectionString(rawUrl);
    }

    var host = Environment.GetEnvironmentVariable("PGHOST") ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
    var db = Environment.GetEnvironmentVariable("PGDATABASE") ?? Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
    var user = Environment.GetEnvironmentVariable("PGUSER") ?? Environment.GetEnvironmentVariable("POSTGRES_USER");
    var pass = Environment.GetEnvironmentVariable("PGPASSWORD") ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

    if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(db) && !string.IsNullOrEmpty(user))
    {
        var npgBuilder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = host,
            Database = db,
            Username = user,
            Password = pass ?? string.Empty,
            SslMode = Npgsql.SslMode.Require,
            Pooling = true,
            Timeout = 15
        };
        return npgBuilder.ConnectionString;
    }

    return null;
}

static string FormatNpgsqlConnectionString(string raw)
{
    if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':');
        var npgBuilder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : null,
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = Npgsql.SslMode.Require,
            Pooling = true,
            Timeout = 15
        };
        return npgBuilder.ConnectionString;
    }
    return raw;
}

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

var builder = WebApplication.CreateBuilder(args);

// Configuration & Connection Strings
var configuration = builder.Configuration;
var defaultConn = configuration.GetConnectionString("DefaultConnection");
bool useSqlite = configuration.GetValue<bool>("UseSqlite") ||
                 (defaultConn != null && defaultConn.Contains(".db", StringComparison.OrdinalIgnoreCase));

if (useSqlite || string.IsNullOrEmpty(defaultConn))
{
    var sqliteConn = configuration.GetConnectionString("SqliteConnection") ?? "Data Source=RentalCarWebsite.db";
    builder.Services.AddDbContext<CustomIdentityContext>(options =>
        options.UseSqlite(sqliteConn));
    builder.Services.AddDbContext<FS23_Group4_ProjectContext>(options =>
        options.UseSqlite(sqliteConn));
}
else
{
    builder.Services.AddDbContext<CustomIdentityContext>(options =>
        options.UseSqlServer(defaultConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));
    builder.Services.AddDbContext<FS23_Group4_ProjectContext>(options =>
        options.UseSqlServer(defaultConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));
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

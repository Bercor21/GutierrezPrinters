using System.Globalization;
using GutierrezPrinters.Web.Data;
using GutierrezPrinters.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Base de datos (PostgreSQL en Supabase) ----------
// La cadena de conexión NO va en el código ni en GitHub: va en "Administrar secretos de usuario".
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'DefaultConnection'. En Visual Studio: clic derecho al proyecto -> " +
        "Administrar secretos de usuario, y pegá ahí la conexión de Supabase.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql
        .MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.Schema)
        // Si la conexión con Supabase falla un momento (pasa al despertar), se reintenta sola
        // en vez de mostrar una página de error.
        .EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

// ---------- 2. Login (ASP.NET Core Identity) ----------
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // el dueño no tiene que entrar a cada rato desde el cel
    options.SlidingExpiration = true;
    options.Cookie.Name = "gp.auth";
});

// ---------- 3. MVC ----------
builder.Services.AddControllersWithViews(options =>
{
    // Todo el sistema pide iniciar sesión, menos lo que tenga [AllowAnonymous].
    options.Filters.Add(new AuthorizeFilter());
    // Protección contra formularios falsos en todos los POST.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Datos del taller (appsettings.json, sección "Shop").
builder.Services.Configure<ShopOptions>(builder.Configuration.GetSection("Shop"));

// Despertador interno: en Azure visita /Home/Ping cada 4 minutos para que el plan gratis
// no apague el sistema (ver Services/KeepAwakeService.cs).
builder.Services.AddHttpClient();
builder.Services.AddHostedService<KeepAwakeService>();

var app = builder.Build();

// ---------- 4. Idioma: español de Costa Rica, con punto decimal ----------
var culture = (CultureInfo)CultureInfo.GetCultureInfo("es-CR").Clone();
culture.NumberFormat.NumberDecimalSeparator = ".";
culture.NumberFormat.NumberGroupSeparator = ",";
culture.NumberFormat.CurrencyDecimalSeparator = ".";
culture.NumberFormat.CurrencyGroupSeparator = ",";
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var localization = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new List<CultureInfo> { culture },
    SupportedUICultures = new List<CultureInfo> { culture }
};
localization.RequestCultureProviders.Clear();

// ---------- 5. Crear/actualizar las tablas y el primer usuario ----------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.GetMigrations().Any())
    {
        // Solo toca las tablas si hay cambios nuevos. Revisarlo es una consulta rápida;
        // así el arranque normal (cuando Azure despierta el sistema) no hace trabajo de más.
        if ((await db.Database.GetPendingMigrationsAsync()).Any())
        {
            await db.Database.MigrateAsync();
        }

        await SeedData.EnsureAdminUserAsync(scope.ServiceProvider, app.Configuration, logger);
    }
    else
    {
        logger.LogWarning("Todavía no hay migraciones. Abrí la Consola del Administrador de paquetes y corré: Add-Migration Inicial");
    }
}

// ---------- 6. Tubería de cada pedido ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization(localization);
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

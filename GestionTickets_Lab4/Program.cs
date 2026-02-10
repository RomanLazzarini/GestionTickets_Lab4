using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GestionTickets_Lab4.Data; // Tu namespace
using GestionTickets_Lab4.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE BASE DE DATOS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. CONFIGURACIÓN DE IDENTITY (USUARIOS Y ROLES)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // A. Reglas de Password relajadas para el examen (Más rápido de escribir)
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 3; // Con poner "123" alcanza
})
.AddRoles<IdentityRole>() // <--- ¡CLAVE! Agregamos soporte para Roles (Admin/Afiliado)
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 3. PIPELINE HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ==============================================================================
// 4. ZONA DE "SEEDING" (CARGA AUTOMÁTICA DE DATOS) - Adaptado de tu compañero
// ==============================================================================
// Esto se ejecuta cada vez que inicias la app. Si los usuarios no existen, los crea.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // A. Crear Roles
        var roles = new[] { "Administrador", "Afiliado" };
        foreach (var rol in roles)
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        // B. Crear Usuario Admin (Si no existe)
        var adminEmail = "admin@tickets.com";
        var adminPass = "123"; // Contraseña fácil para testear

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(adminUser, adminPass);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrador");
            }
        }

        // C. Crear Usuario Afiliado de prueba
        var afiliadoEmail = "pepe@tickets.com";
        var afiliadoPass = "123";

        if (await userManager.FindByEmailAsync(afiliadoEmail) == null)
        {
            var user = new IdentityUser { UserName = afiliadoEmail, Email = afiliadoEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, afiliadoPass);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Afiliado");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocurrió un error al sembrar la base de datos.");
    }
}
// ==============================================================================

app.Run();
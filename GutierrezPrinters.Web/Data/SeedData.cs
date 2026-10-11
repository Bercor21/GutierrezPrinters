using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GutierrezPrinters.Web.Data;

public static class SeedData
{
    // Los administradores pueden crear y quitar usuarios. Los demás solo usan el sistema.
    public const string AdminRole = "Admin";

    // El sistema no tiene registro público: el primer usuario se crea solo,
    // con el correo y la contraseña que pongás en "AdminUser" (secretos de usuario).
    // Los demás se crean desde la pantalla "Usuarios".
    public static async Task EnsureAdminUserAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // 1. Que exista el rol de administrador.
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        // 2. Si no hay ningún usuario, crear el primero con los datos de "AdminUser".
        if (!await userManager.Users.AnyAsync())
        {
            var email = config["AdminUser:Email"];
            var password = config["AdminUser:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("No hay usuarios. Agregá AdminUser:Email y AdminUser:Password en los secretos de usuario para crear el primero.");
                return;
            }

            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                logger.LogInformation("Usuario administrador {Email} creado.", email);
            }
            else
            {
                logger.LogError("No se pudo crear el usuario administrador: {Errors}",
                    string.Join(" ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        // 3. Si todavía nadie es administrador (por ejemplo, usuarios creados antes de que
        //    existieran los roles), los que ya están pasan a serlo.
        var admins = await userManager.GetUsersInRoleAsync(AdminRole);
        if (admins.Count == 0)
        {
            foreach (var user in await userManager.Users.ToListAsync())
            {
                await userManager.AddToRoleAsync(user, AdminRole);
                logger.LogInformation("{Email} ahora es administrador.", user.Email);
            }
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GutierrezPrinters.Web.Data;

public static class SeedData
{
    // El sistema no tiene registro público: el primer usuario se crea solo,
    // con el correo y la contraseña que pongás en "AdminUser" (secretos de usuario).
    public static async Task EnsureAdminUserAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        if (await userManager.Users.AnyAsync())
        {
            return; // Ya hay usuarios, no hacemos nada.
        }

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
        }
    }
}

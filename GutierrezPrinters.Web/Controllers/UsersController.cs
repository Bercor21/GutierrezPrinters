using GutierrezPrinters.Web.Data;
using GutierrezPrinters.Web.Services;
using GutierrezPrinters.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GutierrezPrinters.Web.Controllers;

// Usuarios del sistema: solo los administradores pueden entrar aquí.
[Authorize(Roles = SeedData.AdminRole)]
public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public UsersController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentId = _userManager.GetUserId(User);
        var adminIds = (await _userManager.GetUsersInRoleAsync(SeedData.AdminRole)).Select(u => u.Id).ToHashSet();
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        var rows = users.Select(u => new UserRowViewModel
        {
            Id = u.Id,
            Email = u.Email ?? u.UserName ?? "",
            IsAdmin = adminIds.Contains(u.Id),
            IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
            IsCurrentUser = u.Id == currentId
        }).ToList();

        return View(rows);
    }

    [HttpGet]
    public IActionResult Create() => View(new NewUserViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(NewUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, IdentityErrors.Spanish(error));
            }

            return View(model);
        }

        if (model.IsAdmin)
        {
            await _userManager.AddToRoleAsync(user, SeedData.AdminRole);
        }

        TempData["Message"] = $"Usuario {email} creado. Pasale la contraseña y decile que la cambie al entrar.";
        return RedirectToAction(nameof(Index));
    }

    // Poner una contraseña nueva (por ejemplo, si a alguien se le olvidó).
    [HttpPost]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["Error"] = "Escribí la contraseña nueva.";
            return RedirectToAction(nameof(Index));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(IdentityErrors.Spanish).Distinct());
            return RedirectToAction(nameof(Index));
        }

        // Si estaba bloqueado por intentos fallidos, se desbloquea.
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        TempData["Message"] = $"Contraseña de {user.Email} cambiada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (await _userManager.IsInRoleAsync(user, SeedData.AdminRole))
        {
            if (await IsLastAdmin(user))
            {
                TempData["Error"] = "Tiene que quedar al menos un administrador.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.RemoveFromRoleAsync(user, SeedData.AdminRole);
            await _userManager.UpdateSecurityStampAsync(user); // para que el cambio aplique de una vez
            TempData["Message"] = $"{user.Email} ya no es administrador.";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, SeedData.AdminRole);
            await _userManager.UpdateSecurityStampAsync(user);
            TempData["Message"] = $"{user.Email} ahora es administrador.";
        }

        // Si te lo cambiaste a vos mismo, se actualiza tu sesión para no sacarte.
        if (user.Id == _userManager.GetUserId(User))
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "No podés borrar tu propio usuario.";
            return RedirectToAction(nameof(Index));
        }

        if (await IsLastAdmin(user))
        {
            TempData["Error"] = "Tiene que quedar al menos un administrador.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);
        TempData["Message"] = $"Usuario {user.Email} eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsLastAdmin(IdentityUser user)
    {
        var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
        return admins.Count == 1 && admins[0].Id == user.Id;
    }
}

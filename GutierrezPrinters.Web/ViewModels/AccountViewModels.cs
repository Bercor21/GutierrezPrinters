using System.ComponentModel.DataAnnotations;

namespace GutierrezPrinters.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Escribí tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Escribí tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    [Display(Name = "Mantener la sesión abierta")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Escribí tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Escribí la contraseña nueva.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña nueva")]
    public string NewPassword { get; set; } = "";

    [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir contraseña nueva")]
    public string ConfirmPassword { get; set; } = "";
}

// ---- Usuarios (solo administradores) ----

public class NewUserViewModel
{
    [Required(ErrorMessage = "Escribí el correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Escribí una contraseña.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    [Display(Name = "Administrador (puede crear y quitar usuarios)")]
    public bool IsAdmin { get; set; }
}

public class UserRowViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsAdmin { get; set; }
    public bool IsLockedOut { get; set; }
    public bool IsCurrentUser { get; set; }
}

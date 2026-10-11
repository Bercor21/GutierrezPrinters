using Microsoft.AspNetCore.Identity;

namespace GutierrezPrinters.Web.Services;

// Los errores del login vienen en inglés; aquí se pasan a español.
public static class IdentityErrors
{
    public static string Spanish(IdentityError error) => error.Code switch
    {
        "PasswordMismatch" => "La contraseña actual no es correcta.",
        "PasswordTooShort" => "La contraseña es muy corta: mínimo 8 caracteres.",
        "PasswordRequiresDigit" => "La contraseña debe tener al menos un número.",
        "PasswordRequiresLower" => "La contraseña debe tener al menos una minúscula.",
        "PasswordRequiresUpper" => "La contraseña debe tener al menos una mayúscula.",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe tener al menos un símbolo.",
        "DuplicateUserName" or "DuplicateEmail" => "Ya existe un usuario con ese correo.",
        "InvalidEmail" or "InvalidUserName" => "El correo no es válido.",
        "InvalidToken" => "No se pudo cambiar la contraseña. Intentá de nuevo.",
        _ => error.Description
    };
}

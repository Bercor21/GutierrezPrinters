using System.ComponentModel.DataAnnotations;

namespace GutierrezPrinters.Web.Models;

// El cliente: la persona dueña de una o varias impresoras.
public class Customer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120)]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(150)]
    [Display(Name = "Correo")]
    public string? Email { get; set; }

    [StringLength(30)]
    [Display(Name = "Cédula")]
    public string? IdNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Notas")]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Un cliente puede tener varias impresoras.
    public List<Printer> Printers { get; set; } = new();
}

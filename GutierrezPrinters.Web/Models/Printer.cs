using System.ComponentModel.DataAnnotations;

namespace GutierrezPrinters.Web.Models;

// El equipo: cada impresora física, de cualquier marca, tipo o tamaño.
public class Printer
{
    public int Id { get; set; }

    // Llave foránea: a qué cliente pertenece.
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(60)]
    [Display(Name = "Marca")]
    public string Brand { get; set; } = "";

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(80)]
    [Display(Name = "Modelo")]
    public string Model { get; set; } = "";

    [StringLength(80)]
    [Display(Name = "Número de serie")]
    public string? SerialNumber { get; set; }

    [Display(Name = "Tecnología")]
    public PrinterTechnology Technology { get; set; } = PrinterTechnology.Other;

    [Display(Name = "Tipo")]
    public PrinterCategory Category { get; set; } = PrinterCategory.Other;

    [Display(Name = "Multifuncional (escanea y copia)")]
    public bool IsMultifunction { get; set; }

    [Display(Name = "Imprime a color")]
    public bool IsColor { get; set; }

    [StringLength(500)]
    [Display(Name = "Otros detalles")]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Cada vez que la impresora entra al taller es una orden nueva.
    public List<RepairOrder> Orders { get; set; } = new();

    public string DisplayName => $"{Brand} {Model}".Trim();
}

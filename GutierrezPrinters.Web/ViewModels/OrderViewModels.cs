using System.ComponentModel.DataAnnotations;
using GutierrezPrinters.Web.Models;

namespace GutierrezPrinters.Web.ViewModels;

// Formulario único de ingreso: cliente + equipo + orden en una sola pantalla.
public class NewOrderViewModel
{
    // ---- Cliente ----
    // Si el cliente ya existe, este Id se llena solo al escribir el teléfono.
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string CustomerPhone { get; set; } = "";

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120)]
    [Display(Name = "Nombre del cliente")]
    public string CustomerName { get; set; } = "";

    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(150)]
    [Display(Name = "Correo (opcional)")]
    public string? CustomerEmail { get; set; }

    [StringLength(30)]
    [Display(Name = "Cédula (opcional)")]
    public string? CustomerIdNumber { get; set; }

    // ---- Equipo ----
    // Si es una impresora que ya vino antes, se escoge de la lista y no hay que escribir los datos otra vez.
    [Display(Name = "Equipo")]
    public int? PrinterId { get; set; }

    [StringLength(60)]
    [Display(Name = "Marca")]
    public string? Brand { get; set; }

    [StringLength(80)]
    [Display(Name = "Modelo")]
    public string? Model { get; set; }

    [StringLength(80)]
    [Display(Name = "Número de serie")]
    public string? SerialNumber { get; set; }

    [Display(Name = "Tecnología")]
    public PrinterTechnology Technology { get; set; } = PrinterTechnology.InkTank;

    [Display(Name = "Tipo")]
    public PrinterCategory Category { get; set; } = PrinterCategory.Home;

    [Display(Name = "Multifuncional")]
    public bool IsMultifunction { get; set; }

    [Display(Name = "A color")]
    public bool IsColor { get; set; }

    [StringLength(500)]
    [Display(Name = "Otros detalles del equipo")]
    public string? PrinterDescription { get; set; }

    // ---- Ingreso ----
    public List<string> SelectedAccessories { get; set; } = new();

    [StringLength(150)]
    [Display(Name = "Otros accesorios")]
    public string? OtherAccessories { get; set; }

    [StringLength(1000)]
    [Display(Name = "Estado físico al recibir")]
    public string? PhysicalCondition { get; set; }

    [Required(ErrorMessage = "Escribí la falla que reporta el cliente.")]
    [StringLength(1000)]
    [Display(Name = "Falla reportada por el cliente")]
    public string ReportedIssue { get; set; } = "";

    [Range(0, 99999999, ErrorMessage = "Monto inválido.")]
    [Display(Name = "Cotización (₡)")]
    public decimal? Quote { get; set; }

    [Range(0, 99999999, ErrorMessage = "Monto inválido.")]
    [Display(Name = "Adelanto (₡)")]
    public decimal? Deposit { get; set; }

    [Display(Name = "Fecha estimada de entrega")]
    public DateOnly? EstimatedDate { get; set; }

    // Datos para mostrar (no vienen del formulario).
    public List<Printer> ExistingPrinters { get; set; } = new();

    public static readonly string[] AccessoryOptions =
    {
        "Cable de poder", "Adaptador de corriente", "Cable USB", "Cartuchos / tóner",
        "Bandeja de papel", "Cabezal", "Caja / empaque"
    };
}

// Lo que se puede corregir de una orden después de crearla.
public class OrderEditViewModel
{
    public int Id { get; set; }

    [StringLength(300)]
    [Display(Name = "Accesorios que dejó")]
    public string? Accessories { get; set; }

    [StringLength(1000)]
    [Display(Name = "Estado físico al recibir")]
    public string? PhysicalCondition { get; set; }

    [Required(ErrorMessage = "Escribí la falla que reporta el cliente.")]
    [StringLength(1000)]
    [Display(Name = "Falla reportada")]
    public string ReportedIssue { get; set; } = "";

    [StringLength(2000)]
    [Display(Name = "Diagnóstico")]
    public string? Diagnosis { get; set; }

    [StringLength(1000)]
    [Display(Name = "Pendientes / repuestos")]
    public string? PendingWork { get; set; }

    [Range(0, 99999999, ErrorMessage = "Monto inválido.")]
    [Display(Name = "Cotización (₡)")]
    public decimal? Quote { get; set; }

    [Range(0, 99999999, ErrorMessage = "Monto inválido.")]
    [Display(Name = "Adelanto (₡)")]
    public decimal Deposit { get; set; }

    [Display(Name = "Fecha estimada de entrega")]
    public DateOnly? EstimatedDate { get; set; }

    [Range(0, 3650, ErrorMessage = "Días inválidos.")]
    [Display(Name = "Garantía (días)")]
    public int? WarrantyDays { get; set; }

    // Solo para mostrar el encabezado.
    public RepairOrder? Order { get; set; }
}

public class OrderListViewModel
{
    public string? Query { get; set; }
    public string Filter { get; set; } = "activas";
    public List<RepairOrder> Orders { get; set; } = new();
    public int TotalFound { get; set; }
    public const int MaxResults = 200;
}

public class DashboardViewModel
{
    public int OpenCount { get; set; }
    public Dictionary<OrderStatus, int> CountByStatus { get; set; } = new();
    public List<RepairOrder> Abandoned { get; set; } = new();
    public List<RepairOrder> Overdue { get; set; } = new();
    public List<RepairOrder> Recent { get; set; } = new();
    public int AbandonedAfterDays { get; set; }
}

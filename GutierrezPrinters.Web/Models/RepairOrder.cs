using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GutierrezPrinters.Web.Models;

// La orden de servicio: cada vez que una impresora entra al taller.
// El Id es el número que va en la etiqueta: 42 se muestra como #0042.
public class RepairOrder
{
    public int Id { get; set; }

    public int PrinterId { get; set; }
    public Printer? Printer { get; set; }

    [Display(Name = "Estado")]
    public OrderStatus Status { get; set; } = OrderStatus.Received;

    // ---- Cómo venía la impresora ----
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

    // ---- Lo que hace el técnico ----
    [StringLength(2000)]
    [Display(Name = "Diagnóstico")]
    public string? Diagnosis { get; set; }

    [StringLength(1000)]
    [Display(Name = "Pendientes / repuestos")]
    public string? PendingWork { get; set; }

    // ---- Plata (colones) ----
    [Precision(12, 2)]
    [Range(0, 99999999, ErrorMessage = "Monto inválido.")]
    [Display(Name = "Cotización (₡)")]
    public decimal? Quote { get; set; }

    [Precision(12, 2)]
    [Range(0, 99999999, ErrorMessage = "Monto inválido.")]
    [Display(Name = "Adelanto (₡)")]
    public decimal Deposit { get; set; }

    // ---- Fechas (se guardan en UTC) ----
    [Display(Name = "Fecha de ingreso")]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Fecha estimada")]
    public DateOnly? EstimatedDate { get; set; }

    [Display(Name = "Lista desde")]
    public DateTime? ReadyAt { get; set; }

    [Display(Name = "Fecha de entrega")]
    public DateTime? DeliveredAt { get; set; }

    [Range(0, 3650, ErrorMessage = "Días inválidos.")]
    [Display(Name = "Garantía (días)")]
    public int? WarrantyDays { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ---- Calculados (no se guardan en la base de datos) ----
    public string Number => FormatNumber(Id);

    public decimal? Balance => Quote.HasValue ? Quote.Value - Deposit : null;

    // Si no se reparó, el cliente paga solo el diagnóstico (menos el adelanto).
    // Si se reparó, el diagnóstico va incluido en la cotización.
    public bool ChargesDiagnosisOnly => Status is OrderStatus.NotRepaired or OrderStatus.ReturnedUnrepaired;

    public decimal? AmountDue(decimal diagnosisFee) =>
        ChargesDiagnosisOnly ? Math.Max(0, diagnosisFee - Deposit) : Balance;

    // Ya salió del taller (entregada o reciclada): no cuenta como pendiente.
    public bool IsClosed => IsClosedStatus(Status);

    // Terminó el trabajo y la impresora espera a que el cliente la retire.
    public bool IsAwaitingPickup => IsAwaitingPickupStatus(Status);

    public static bool IsClosedStatus(OrderStatus status) =>
        status is OrderStatus.Delivered or OrderStatus.ReturnedUnrepaired or OrderStatus.Recycled;

    public static bool IsAwaitingPickupStatus(OrderStatus status) =>
        status is OrderStatus.Ready or OrderStatus.NotRepaired;

    public DateTime? WarrantyUntil =>
        DeliveredAt.HasValue && WarrantyDays is > 0 ? DeliveredAt.Value.AddDays(WarrantyDays.Value) : null;

    public static string FormatNumber(int id) => "#" + id.ToString("D4");
}

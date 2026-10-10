using System.ComponentModel.DataAnnotations;

namespace GutierrezPrinters.Web.Models;

// Un enum es una lista cerrada de opciones. En la base de datos se guarda como texto
// (por ejemplo "InRepair") y en pantalla se muestra el nombre en español del [Display].

public enum OrderStatus
{
    [Display(Name = "Recibida")] Received,
    [Display(Name = "En diagnóstico")] Diagnosis,
    [Display(Name = "Esperando aprobación")] AwaitingApproval,
    [Display(Name = "Esperando repuesto")] AwaitingParts,
    [Display(Name = "En reparación")] InRepair,
    [Display(Name = "Lista para entregar")] Ready,
    [Display(Name = "Entregada")] Delivered,
    [Display(Name = "No reparada")] NotRepaired
}

public enum PrinterTechnology
{
    [Display(Name = "Tinta (cartuchos)")] InkCartridge,
    [Display(Name = "Tanque de tinta")] InkTank,
    [Display(Name = "Láser (tóner)")] Laser,
    [Display(Name = "Matricial")] DotMatrix,
    [Display(Name = "Térmica")] Thermal,
    [Display(Name = "Sublimación")] Sublimation,
    [Display(Name = "Otra")] Other
}

public enum PrinterCategory
{
    [Display(Name = "Hogar")] Home,
    [Display(Name = "Oficina")] Office,
    [Display(Name = "Portátil")] Portable,
    [Display(Name = "Producción / fotocopiadora")] Production,
    [Display(Name = "Gran formato (plotter)")] WideFormat,
    [Display(Name = "Punto de venta")] PointOfSale,
    [Display(Name = "Etiquetas")] Label,
    [Display(Name = "Otra")] Other
}

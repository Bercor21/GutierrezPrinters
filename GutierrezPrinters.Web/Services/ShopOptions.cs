namespace GutierrezPrinters.Web.Services;

// Datos y reglas del taller que salen en la boleta, la etiqueta y los mensajes.
// Se cambian en appsettings.json, sección "Shop", sin tocar código.
public class ShopOptions
{
    public string Name { get; set; } = "Gutierrez Printers";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string WhatsAppCountryCode { get; set; } = "506";

    // Dirección pública del sistema (cuando esté en Azure), por ejemplo
    // "https://gutierrezprinters.azurewebsites.net". El QR de la etiqueta usa esta dirección
    // para que funcione al escanearlo con el celular aunque la etiqueta se imprima desde otra compu.
    // Vacío = se usa la dirección desde donde se abrió la página.
    public string PublicUrl { get; set; } = "";

    // Etiqueta adhesiva: 4 × 6 pulgadas = 101.6 × 152.4 mm (vertical).
    public double LabelWidthMm { get; set; } = 101.6;
    public double LabelHeightMm { get; set; } = 152.4;

    // Garantía que se pone sola al entregar una impresora reparada (180 días = 6 meses).
    public int WarrantyDays { get; set; } = 180;

    // Lo que cuesta el diagnóstico. Se cobra siempre al retirar; si se repara, la reparación va aparte.
    public decimal DiagnosisFee { get; set; } = 7000;

    // Días que una impresora puede estar lista sin que la retiren antes de avisar
    // que se va a enviar a reciclaje.
    public int AbandonedAfterDays { get; set; } = 30;

    // Condiciones que salen en la boleta del cliente. Se pueden usar estas marcas y se cambian solas:
    // {diagnostico} -> ₡7,000   {garantia} -> 6 meses   {dias} -> 30
    public List<string> Terms { get; set; } = new();

    public IEnumerable<string> TermsText() => Terms
        .Where(t => !string.IsNullOrWhiteSpace(t))
        .Select(t => t
            .Replace("{diagnostico}", Display.Money(DiagnosisFee))
            .Replace("{garantia}", Display.Warranty(WarrantyDays))
            .Replace("{dias}", AbandonedAfterDays.ToString()));
}

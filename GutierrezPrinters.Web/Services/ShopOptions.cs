namespace GutierrezPrinters.Web.Services;

// Datos del taller que salen en la hoja, la etiqueta y los mensajes.
// Se cambian en appsettings.json, sección "Shop", sin tocar código.
public class ShopOptions
{
    public string Name { get; set; } = "Gutierrez Printers";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string WhatsAppCountryCode { get; set; } = "506";

    // Tamaño de la etiqueta adhesiva en milímetros.
    public int LabelWidthMm { get; set; } = 62;
    public int LabelHeightMm { get; set; } = 40;

    // Días que una orden puede estar "Lista" sin que la retiren antes de alertar.
    public int AbandonedAfterDays { get; set; } = 30;

    // Condiciones que salen al pie de la hoja de ingreso.
    public List<string> Terms { get; set; } = new();
}

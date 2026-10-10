using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using GutierrezPrinters.Web.Models;

namespace GutierrezPrinters.Web.Services;

// Ayudantes para mostrar datos bonitos en pantalla: plata, fechas, estados.
public static class Display
{
    // ---- Hora de Costa Rica (UTC-6, sin horario de verano) ----
    private static readonly TimeZoneInfo CostaRica = FindCostaRicaZone();

    private static TimeZoneInfo FindCostaRicaZone()
    {
        foreach (var id in new[] { "America/Costa_Rica", "Central America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Costa Rica", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica");
    }

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), CostaRica);

    public static DateOnly Today => DateOnly.FromDateTime(ToLocal(DateTime.UtcNow));

    // ---- Formatos ----
    public static string Date(DateTime? utc) => utc.HasValue ? ToLocal(utc.Value).ToString("dd/MM/yyyy") : "—";

    public static string Date(DateOnly? date) => date.HasValue ? date.Value.ToString("dd/MM/yyyy") : "—";

    public static string DateTimeShort(DateTime? utc) =>
        utc.HasValue ? ToLocal(utc.Value).ToString("dd/MM/yyyy h:mm tt", CultureInfo.CurrentCulture) : "—";

    public static string Money(decimal? amount) =>
        amount.HasValue ? "₡" + amount.Value.ToString("N0", CultureInfo.CurrentCulture) : "—";

    public static string OrEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? "—" : text;

    // Lee el [Display(Name = "...")] de un enum: OrderStatus.Ready -> "Lista para entregar".
    public static string Name(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var attribute = member?.GetCustomAttribute<DisplayAttribute>();
        return attribute?.GetName() ?? value.ToString();
    }

    // Color de cada estado (ver site.css): cian = se está trabajando, amarillo = esperando a alguien,
    // verde = lista, magenta = ojo, gris = recién recibida o ya cerrada.
    public static string StatusTone(OrderStatus status) => status switch
    {
        OrderStatus.Diagnosis or OrderStatus.InRepair => "tone-cyan",
        OrderStatus.AwaitingApproval or OrderStatus.AwaitingParts => "tone-yellow",
        OrderStatus.Ready => "tone-green",
        OrderStatus.NotRepaired => "tone-magenta",
        OrderStatus.Delivered or OrderStatus.ReturnedUnrepaired or OrderStatus.Recycled => "tone-closed",
        _ => "tone-neutral"
    };

    // 180 -> "6 meses", 45 -> "45 días".
    public static string Warranty(int? days)
    {
        if (days is not > 0)
        {
            return "Sin garantía";
        }

        return days.Value % 30 == 0
            ? (days.Value == 30 ? "1 mes" : $"{days.Value / 30} meses")
            : (days.Value == 1 ? "1 día" : $"{days.Value} días");
    }

    // Números para CSS (siempre con punto decimal, sin importar el idioma).
    public static string Css(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    // Cuántos días han pasado desde una fecha (en hora de Costa Rica).
    public static int DaysSince(DateTime? utc) =>
        utc.HasValue ? Today.DayNumber - DateOnly.FromDateTime(ToLocal(utc.Value)).DayNumber : 0;

    // Solo los números de un teléfono: "8888-8888" -> "88888888".
    public static string Digits(string? text) => new((text ?? "").Where(char.IsDigit).ToArray());

    // Para el buscador: si lo escrito parece un teléfono ("8888-8888", "+506 8888 8888") devuelve
    // solo los números; si tiene letras (por ejemplo una serie "CN1234") devuelve vacío.
    public static string PhoneDigits(string? text)
    {
        var value = (text ?? "").Trim();
        return value.Length > 0 && value.All(ch => char.IsDigit(ch) || ch is ' ' or '-' or '+' or '(' or ')')
            ? Digits(value)
            : "";
    }
}

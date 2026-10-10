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

    // Color de Bootstrap para cada estado.
    public static string BadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Received => "text-bg-secondary",
        OrderStatus.Diagnosis => "text-bg-info",
        OrderStatus.AwaitingApproval => "text-bg-warning",
        OrderStatus.AwaitingParts => "text-bg-warning",
        OrderStatus.InRepair => "text-bg-primary",
        OrderStatus.Ready => "text-bg-success",
        OrderStatus.Delivered => "text-bg-dark",
        OrderStatus.NotRepaired => "text-bg-danger",
        _ => "text-bg-secondary"
    };

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

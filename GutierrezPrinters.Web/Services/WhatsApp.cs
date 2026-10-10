using GutierrezPrinters.Web.Models;

namespace GutierrezPrinters.Web.Services;

// Arma el enlace wa.me con un mensaje listo según el estado de la orden.
// No necesita ninguna cuenta ni API: abre WhatsApp con el texto escrito y el dueño solo le da enviar.
public static class WhatsApp
{
    public static string? Link(string? phone, string message, string countryCode)
    {
        var digits = Display.Digits(phone);
        if (digits.Length == 0)
        {
            return null;
        }

        if (digits.Length == 8)
        {
            digits = countryCode + digits; // 8888-8888 -> 50688888888
        }

        return "https://wa.me/" + digits + "?text=" + Uri.EscapeDataString(message);
    }

    public static string Link(Customer customer, string message, ShopOptions shop) =>
        Link(customer.Phone, message, shop.WhatsAppCountryCode) ?? "#";

    public static string OrderMessage(RepairOrder order, ShopOptions shop)
    {
        var customer = order.Printer?.Customer;
        var firstName = (customer?.Name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var printer = order.Printer?.DisplayName ?? "su impresora";
        var hello = $"Hola {firstName}, le saluda {shop.Name}.";
        var reference = $"su impresora {printer} (orden {order.Number})";

        return order.Status switch
        {
            OrderStatus.Received =>
                $"{hello} Recibimos {reference}. Le avisaremos apenas tengamos el diagnóstico.",
            OrderStatus.Diagnosis =>
                $"{hello} Le contamos que {reference} está en diagnóstico.",
            OrderStatus.AwaitingApproval =>
                $"{hello} Ya revisamos {reference}." +
                (string.IsNullOrWhiteSpace(order.Diagnosis) ? "" : $" Diagnóstico: {order.Diagnosis.Trim()}.") +
                (order.Quote.HasValue ? $" Cotización: {Display.Money(order.Quote)}." : "") +
                " ¿Desea que procedamos con la reparación?",
            OrderStatus.AwaitingParts =>
                $"{hello} Le contamos que {reference} está esperando un repuesto. Le avisamos apenas llegue.",
            OrderStatus.InRepair =>
                $"{hello} Le contamos que {reference} ya está en reparación.",
            OrderStatus.Ready =>
                $"{hello} ¡Buenas noticias! {Capitalize(reference)} ya está lista para retirar." +
                (order.Balance is > 0 ? $" Saldo pendiente: {Display.Money(order.Balance)}." : ""),
            OrderStatus.Delivered =>
                $"{hello} Gracias por su confianza. Le confirmamos la entrega de {reference}." +
                (order.WarrantyUntil.HasValue ? $" La garantía es válida hasta el {Display.Date(order.WarrantyUntil)}." : ""),
            OrderStatus.NotRepaired =>
                $"{hello} Lamentablemente {reference} no se pudo reparar. Puede pasar a retirarla cuando guste.",
            _ => hello
        };
    }

    private static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0]) + text[1..];
}

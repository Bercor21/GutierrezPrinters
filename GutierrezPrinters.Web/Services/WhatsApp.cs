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
        var (hello, reference) = Parts(order, shop);
        var due = order.AmountDue(shop.DiagnosisFee);

        return order.Status switch
        {
            OrderStatus.Received =>
                $"{hello} Recibimos {reference}. El diagnóstico tiene un costo de {Display.Money(shop.DiagnosisFee)}; " +
                "le avisaremos apenas lo tengamos.",
            OrderStatus.Diagnosis =>
                $"{hello} Le contamos que {reference} está en diagnóstico.",
            OrderStatus.AwaitingApproval =>
                $"{hello} Ya revisamos {reference}." +
                (string.IsNullOrWhiteSpace(order.Diagnosis) ? "" : $" Diagnóstico: {order.Diagnosis.Trim()}.") +
                (order.Quote.HasValue
                    ? $" La reparación cuesta {Display.Money(order.Quote)}, más el diagnóstico de {Display.Money(shop.DiagnosisFee)}."
                    : "") +
                " ¿Desea que procedamos con la reparación?",
            OrderStatus.AwaitingParts =>
                $"{hello} Le contamos que {reference} está esperando un repuesto. Le avisamos apenas llegue.",
            OrderStatus.InRepair =>
                $"{hello} Le contamos que {reference} ya está en reparación.",
            OrderStatus.Ready =>
                $"{hello} ¡Buenas noticias! {Capitalize(reference)} ya está lista para retirar." +
                (order.HasFinalPrice && due > 0 ? $" Total a cancelar: {Display.Money(due)}." : "") +
                (shop.WarrantyDays > 0 ? $" La reparación tiene {Display.Warranty(shop.WarrantyDays)} de garantía." : ""),
            OrderStatus.NotRepaired =>
                $"{hello} Lamentablemente {reference} no se pudo reparar." +
                (string.IsNullOrWhiteSpace(order.Diagnosis) ? "" : $" Diagnóstico: {order.Diagnosis.Trim()}.") +
                $" El diagnóstico tiene un costo de {Display.Money(shop.DiagnosisFee)}" +
                (order.Deposit > 0 ? $"; con su adelanto, el saldo es {Display.Money(due)}." : ".") +
                " Puede pasar a retirarla cuando guste.",
            OrderStatus.Delivered =>
                $"{hello} Gracias por su confianza. Le confirmamos la entrega de {reference}." +
                (order.WarrantyUntil.HasValue ? $" La garantía es válida hasta el {Display.Date(order.WarrantyUntil)}." : ""),
            OrderStatus.ReturnedUnrepaired =>
                $"{hello} Gracias por su confianza. Le confirmamos la entrega de {reference}.",
            OrderStatus.Recycled =>
                $"{hello} Le informamos que {reference} fue enviada a reciclaje porque no se retiró a tiempo, " +
                "según las condiciones aceptadas al dejarla.",
            _ => hello
        };
    }

    // Recordatorio para las que llevan mucho tiempo listas sin que las retiren.
    public static string PickupReminder(RepairOrder order, ShopOptions shop)
    {
        var (hello, reference) = Parts(order, shop);
        var due = order.AmountDue(shop.DiagnosisFee);

        return $"{hello} Le recordamos que {reference} está lista para retirar desde el {Display.Date(order.ReadyAt)}." +
               (order.HasFinalPrice && due > 0 ? $" Total a cancelar: {Display.Money(due)}." : "") +
               " Le pedimos pasar a retirarla lo antes posible; de lo contrario será enviada a reciclaje, " +
               "según las condiciones aceptadas al dejarla.";
    }

    private static (string Hello, string Reference) Parts(RepairOrder order, ShopOptions shop)
    {
        var customer = order.Printer?.Customer;
        var firstName = (customer?.Name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var printer = order.Printer?.DisplayName ?? "";
        var hello = $"Hola {firstName}, le saluda {shop.Name}.";
        var reference = string.IsNullOrWhiteSpace(printer)
            ? $"su impresora (orden {order.Number})"
            : $"su impresora {printer} (orden {order.Number})";
        return (hello, reference);
    }

    private static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0]) + text[1..];
}

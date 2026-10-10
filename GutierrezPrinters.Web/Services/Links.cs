using Microsoft.AspNetCore.Mvc;

namespace GutierrezPrinters.Web.Services;

public static class Links
{
    // Dirección completa de una orden para el QR.
    // Si en appsettings.json está "PublicUrl" (la dirección de Azure), se usa esa;
    // si no, la de la página actual (en tu compu sería https://localhost:xxxx).
    public static string Order(IUrlHelper url, ShopOptions shop, int orderId)
    {
        var path = url.Action("Details", "Orders", new { id = orderId }) ?? $"/Orders/Details/{orderId}";

        if (!string.IsNullOrWhiteSpace(shop.PublicUrl))
        {
            return shop.PublicUrl.TrimEnd('/') + path;
        }

        var request = url.ActionContext.HttpContext.Request;
        return $"{request.Scheme}://{request.Host}{path}";
    }
}

using System.Diagnostics;
using GutierrezPrinters.Web.Data;
using GutierrezPrinters.Web.Models;
using GutierrezPrinters.Web.Services;
using GutierrezPrinters.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GutierrezPrinters.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly ShopOptions _shop;

    public HomeController(AppDbContext db, IOptions<ShopOptions> shop)
    {
        _db = db;
        _shop = shop.Value;
    }

    // Panel de inicio: resumen del taller y alertas.
    public async Task<IActionResult> Index()
    {
        var abandonedLimit = DateTime.UtcNow.AddDays(-_shop.AbandonedAfterDays);
        var today = Display.Today;

        // Todo lo que sigue físicamente en el taller (no entregadas ni recicladas).
        var openOrders = await _db.RepairOrders
            .AsNoTracking()
            .Include(o => o.Printer).ThenInclude(p => p!.Customer)
            .Where(o => o.Status != OrderStatus.Delivered &&
                        o.Status != OrderStatus.ReturnedUnrepaired &&
                        o.Status != OrderStatus.Recycled)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        var model = new DashboardViewModel
        {
            OpenCount = openOrders.Count,
            CountByStatus = openOrders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count()),
            // Listas (o no reparables) que nadie ha venido a retirar.
            Abandoned = openOrders
                .Where(o => o.IsAwaitingPickup && o.ReadyAt.HasValue && o.ReadyAt.Value < abandonedLimit)
                .OrderBy(o => o.ReadyAt)
                .ToList(),
            // Todavía en trabajo y ya pasó la fecha que se le dijo al cliente.
            Overdue = openOrders
                .Where(o => !o.IsAwaitingPickup && o.EstimatedDate.HasValue && o.EstimatedDate.Value < today)
                .OrderBy(o => o.EstimatedDate)
                .ToList(),
            Recent = openOrders.Take(8).ToList(),
            AbandonedAfterDays = _shop.AbandonedAfterDays
        };

        return View(model);
    }

    // "Despertador": el propio sistema la visita cada 4 minutos (Services/KeepAwakeService.cs)
    // y GitHub una vez al día de respaldo (.github/workflows/mantener-activo.yml).
    // Así Azure no lo apaga y Supabase no pausa la base de datos.
    // Solo LEE la base (pregunta si hay clientes); no cambia ni muestra ningún dato.
    [AllowAnonymous]
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Ping()
    {
        await _db.Customers.AnyAsync();
        return Content("ok");
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

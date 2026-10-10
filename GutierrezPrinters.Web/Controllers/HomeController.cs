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

        var openOrders = await _db.RepairOrders
            .AsNoTracking()
            .Include(o => o.Printer).ThenInclude(p => p!.Customer)
            .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.NotRepaired)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        var model = new DashboardViewModel
        {
            OpenCount = openOrders.Count,
            CountByStatus = openOrders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count()),
            Abandoned = openOrders
                .Where(o => o.Status == OrderStatus.Ready && o.ReadyAt.HasValue && o.ReadyAt.Value < abandonedLimit)
                .OrderBy(o => o.ReadyAt)
                .ToList(),
            Overdue = openOrders
                .Where(o => o.Status != OrderStatus.Ready && o.EstimatedDate.HasValue && o.EstimatedDate.Value < today)
                .OrderBy(o => o.EstimatedDate)
                .ToList(),
            Recent = openOrders.Take(8).ToList(),
            AbandonedAfterDays = _shop.AbandonedAfterDays
        };

        return View(model);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

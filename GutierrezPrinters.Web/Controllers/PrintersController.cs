using GutierrezPrinters.Web.Data;
using GutierrezPrinters.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GutierrezPrinters.Web.Controllers;

public class PrintersController : Controller
{
    private readonly AppDbContext _db;

    public PrintersController(AppDbContext db)
    {
        _db = db;
    }

    // Ficha del equipo con su historial: todas las veces que ha entrado al taller.
    public async Task<IActionResult> Details(int id)
    {
        var printer = await _db.Printers
            .AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Orders)
            .FirstOrDefaultAsync(p => p.Id == id);

        return printer == null ? NotFound() : View(printer);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var printer = await _db.Printers.Include(p => p.Customer).FirstOrDefaultAsync(p => p.Id == id);
        return printer == null ? NotFound() : View(printer);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id,
        [Bind("Brand,Model,SerialNumber,Technology,Category,IsMultifunction,IsColor,Description")] Printer input)
    {
        var printer = await _db.Printers.Include(p => p.Customer).FirstOrDefaultAsync(p => p.Id == id);
        if (printer == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            input.Id = id;
            input.Customer = printer.Customer;
            return View(input);
        }

        printer.Brand = input.Brand.Trim();
        printer.Model = input.Model.Trim();
        printer.SerialNumber = string.IsNullOrWhiteSpace(input.SerialNumber) ? null : input.SerialNumber.Trim();
        printer.Technology = input.Technology;
        printer.Category = input.Category;
        printer.IsMultifunction = input.IsMultifunction;
        printer.IsColor = input.IsColor;
        printer.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        await _db.SaveChangesAsync();

        TempData["Message"] = "Equipo actualizado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // Borra la impresora y todas sus órdenes. Solo administradores; no se puede deshacer.
    [HttpPost]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var printer = await _db.Printers
            .Include(p => p.Orders)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (printer == null)
        {
            return NotFound();
        }

        var customerId = printer.CustomerId;
        var name = printer.DisplayName;
        var orderCount = printer.Orders.Count;

        foreach (var order in printer.Orders.ToList())
        {
            _db.RepairOrders.Remove(order);
        }
        _db.Printers.Remove(printer);
        await _db.SaveChangesAsync(); // todo junto: o se borra todo o nada

        TempData["Message"] = orderCount == 0
            ? $"Impresora {name} eliminada."
            : $"Impresora {name} eliminada, con {orderCount} {(orderCount == 1 ? "orden" : "órdenes")}.";
        return RedirectToAction("Details", "Customers", new { id = customerId });
    }
}

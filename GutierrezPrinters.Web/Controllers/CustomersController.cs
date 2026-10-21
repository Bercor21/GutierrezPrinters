using GutierrezPrinters.Web.Data;
using GutierrezPrinters.Web.Models;
using GutierrezPrinters.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GutierrezPrinters.Web.Controllers;

public class CustomersController : Controller
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    // Lista de clientes con buscador por nombre, teléfono, correo o cédula.
    public async Task<IActionResult> Index(string? q)
    {
        IQueryable<Customer> query = _db.Customers.AsNoTracking().Include(c => c.Printers);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var lower = q.Trim().ToLower();
            var digits = Display.PhoneDigits(q);
            query = query.Where(c =>
                c.Name.ToLower().Contains(lower) ||
                (digits.Length >= 4 && c.Phone.Replace("-", "").Replace(" ", "").Contains(digits)) ||
                (c.Email != null && c.Email.ToLower().Contains(lower)) ||
                (c.IdNumber != null && c.IdNumber.Contains(lower)));
        }

        ViewData["Query"] = q;
        var customers = await query.OrderBy(c => c.Name).Take(300).ToListAsync();
        return View(customers);
    }

    // Ficha del cliente: sus impresoras y el historial de órdenes.
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .Include(c => c.Printers).ThenInclude(p => p.Orders)
            .FirstOrDefaultAsync(c => c.Id == id);

        return customer == null ? NotFound() : View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        return customer == null ? NotFound() : View(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Phone,Email,IdNumber,Notes")] Customer input)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            input.Id = id;
            return View(input);
        }

        customer.Name = input.Name.Trim();
        customer.Phone = input.Phone.Trim();
        customer.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        customer.IdNumber = string.IsNullOrWhiteSpace(input.IdNumber) ? null : input.IdNumber.Trim();
        customer.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        await _db.SaveChangesAsync();

        TempData["Message"] = "Cliente actualizado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // Borra el cliente con todas sus impresoras y órdenes. Solo administradores; no se puede deshacer.
    [HttpPost]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.Printers).ThenInclude(p => p.Orders)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            return NotFound();
        }

        var printerCount = customer.Printers.Count;
        var orderCount = customer.Printers.Sum(p => p.Orders.Count);

        foreach (var printer in customer.Printers.ToList())
        {
            foreach (var order in printer.Orders.ToList())
            {
                _db.RepairOrders.Remove(order);
            }
            _db.Printers.Remove(printer);
        }
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync(); // todo junto: o se borra todo o nada

        TempData["Message"] = printerCount == 0
            ? $"Cliente {customer.Name} eliminado."
            : $"Cliente {customer.Name} eliminado, con {printerCount} {(printerCount == 1 ? "impresora" : "impresoras")} " +
              $"y {orderCount} {(orderCount == 1 ? "orden" : "órdenes")}.";
        return RedirectToAction(nameof(Index));
    }

    // Lo usa el formulario de ingreso: al escribir el teléfono, trae al cliente si ya existe.
    // GET /Customers/Lookup?phone=88888888  -> JSON
    [HttpGet]
    public async Task<IActionResult> Lookup(string? phone)
    {
        var digits = Display.Digits(phone);
        if (digits.Length < 8)
        {
            return Json(new { found = false });
        }

        // Si escribieron con 506 adelante, se busca con los últimos 8 dígitos.
        var lastEight = digits[^8..];
        var customer = await _db.Customers
            .AsNoTracking()
            .Include(c => c.Printers)
            .FirstOrDefaultAsync(c => c.Phone.Replace("-", "").Replace(" ", "").Replace("+", "").EndsWith(lastEight));

        if (customer == null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            id = customer.Id,
            name = customer.Name,
            phone = customer.Phone,
            email = customer.Email,
            idNumber = customer.IdNumber,
            printers = customer.Printers
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    id = p.Id,
                    label = p.DisplayName + (string.IsNullOrEmpty(p.SerialNumber) ? "" : " · S/N " + p.SerialNumber)
                })
        });
    }
}

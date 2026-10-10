using GutierrezPrinters.Web.Data;
using GutierrezPrinters.Web.Models;
using GutierrezPrinters.Web.Services;
using GutierrezPrinters.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GutierrezPrinters.Web.Controllers;

// Las órdenes de servicio: el corazón del sistema.
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly ShopOptions _shop;

    public OrdersController(AppDbContext db, IOptions<ShopOptions> shop)
    {
        _db = db;
        _shop = shop.Value;
    }

    // ---------- Lista + buscador ----------
    // /Orders?q=ana  -> busca por número de orden, cliente, teléfono, marca, modelo o serie.
    public async Task<IActionResult> Index(string? q, string? estado)
    {
        // Si se busca algo sin escoger estado, se busca en TODAS las órdenes (también las entregadas).
        var filter = !string.IsNullOrWhiteSpace(estado) ? estado
            : string.IsNullOrWhiteSpace(q) ? "activas" : "todas";

        IQueryable<RepairOrder> query = _db.RepairOrders
            .AsNoTracking()
            .Include(o => o.Printer).ThenInclude(p => p!.Customer);

        if (filter == "activas")
        {
            // Todo lo que sigue físicamente en el taller.
            query = query.Where(o => o.Status != OrderStatus.Delivered &&
                                     o.Status != OrderStatus.ReturnedUnrepaired &&
                                     o.Status != OrderStatus.Recycled);
        }
        else if (filter == "cerradas")
        {
            query = query.Where(o => o.Status == OrderStatus.Delivered ||
                                     o.Status == OrderStatus.ReturnedUnrepaired ||
                                     o.Status == OrderStatus.Recycled);
        }
        else if (filter == "por-retirar")
        {
            query = query.Where(o => o.Status == OrderStatus.Ready || o.Status == OrderStatus.NotRepaired);
        }
        else if (filter == "por-diagnosticar")
        {
            query = query.Where(o => o.Status == OrderStatus.Received || o.Status == OrderStatus.Diagnosis);
        }
        else if (filter == "esperando")
        {
            query = query.Where(o => o.Status == OrderStatus.AwaitingApproval || o.Status == OrderStatus.AwaitingParts);
        }
        else if (Enum.TryParse<OrderStatus>(filter, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var lower = term.ToLower();
            var compact = lower.Replace("-", "").Replace(" ", "");
            var digits = Display.PhoneDigits(term);

            // "#0042", "0042" o "42" -> orden 42
            int? number = null;
            var numberText = term.TrimStart('#');
            if (numberText.Length is > 0 and <= 9 && numberText.All(char.IsDigit))
            {
                number = int.Parse(numberText);
            }

            query = query.Where(o =>
                (number != null && o.Id == number) ||
                o.Printer!.Customer!.Name.ToLower().Contains(lower) ||
                (digits.Length >= 4 && o.Printer!.Customer!.Phone.Replace("-", "").Replace(" ", "").Contains(digits)) ||
                (o.Printer!.Brand + " " + o.Printer!.Model).ToLower().Contains(lower) ||
                o.Printer!.Model.ToLower().Replace("-", "").Replace(" ", "").Contains(compact) ||
                (o.Printer!.SerialNumber != null &&
                 o.Printer!.SerialNumber.ToLower().Replace("-", "").Replace(" ", "").Contains(compact)));
        }

        var model = new OrderListViewModel
        {
            Query = q,
            Filter = filter,
            TotalFound = await query.CountAsync(),
            Orders = await query.OrderByDescending(o => o.Id).Take(OrderListViewModel.MaxResults).ToListAsync()
        };

        return View(model);
    }

    // ---------- Ingreso de una impresora (formulario único) ----------
    [HttpGet]
    public async Task<IActionResult> Create(int? printerId, int? customerId)
    {
        var model = new NewOrderViewModel();

        if (printerId.HasValue)
        {
            var printer = await _db.Printers.Include(p => p.Customer).FirstOrDefaultAsync(p => p.Id == printerId.Value);
            if (printer?.Customer != null)
            {
                FillCustomer(model, printer.Customer);
                model.PrinterId = printer.Id;
                model.ExistingPrinters = await PrintersOf(printer.CustomerId);
            }
        }
        else if (customerId.HasValue)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId.Value);
            if (customer != null)
            {
                FillCustomer(model, customer);
                model.ExistingPrinters = await PrintersOf(customer.Id);
            }
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewOrderViewModel form)
    {
        // 1. ¿El cliente ya existe? Primero por Id, si no por teléfono.
        Customer? customer = null;
        if (form.CustomerId.HasValue)
        {
            customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == form.CustomerId.Value);
        }

        var phoneDigits = Display.Digits(form.CustomerPhone);
        if (customer == null && phoneDigits.Length > 0)
        {
            customer = await _db.Customers.FirstOrDefaultAsync(c =>
                c.Phone.Replace("-", "").Replace(" ", "").Replace("+", "") == phoneDigits);
        }

        // 2. ¿La impresora ya vino antes? (escogida de la lista o con el mismo número de serie)
        Printer? printer = null;
        if (form.PrinterId.HasValue && customer != null)
        {
            printer = await _db.Printers.FirstOrDefaultAsync(p => p.Id == form.PrinterId.Value && p.CustomerId == customer.Id);
        }

        var serial = form.SerialNumber?.Trim();
        if (printer == null && customer != null && !string.IsNullOrEmpty(serial))
        {
            var serialUpper = serial.ToUpper();
            printer = await _db.Printers.FirstOrDefaultAsync(p =>
                p.CustomerId == customer.Id && p.SerialNumber != null && p.SerialNumber.ToUpper() == serialUpper);
        }

        if (printer == null)
        {
            if (string.IsNullOrWhiteSpace(form.Brand))
            {
                ModelState.AddModelError(nameof(form.Brand), "La marca es obligatoria.");
            }

            if (string.IsNullOrWhiteSpace(form.Model))
            {
                ModelState.AddModelError(nameof(form.Model), "El modelo es obligatorio.");
            }
        }

        if (!ModelState.IsValid)
        {
            form.ExistingPrinters = customer != null ? await PrintersOf(customer.Id) : new List<Printer>();
            return View(form);
        }

        // 3. Guardar cliente (nuevo o actualizado).
        if (customer == null)
        {
            customer = new Customer();
            _db.Customers.Add(customer);
        }

        customer.Name = form.CustomerName.Trim();
        if (Display.Digits(customer.Phone) != phoneDigits)
        {
            customer.Phone = form.CustomerPhone.Trim();
        }
        if (!string.IsNullOrWhiteSpace(form.CustomerEmail))
        {
            customer.Email = form.CustomerEmail.Trim();
        }

        if (!string.IsNullOrWhiteSpace(form.CustomerIdNumber))
        {
            customer.IdNumber = form.CustomerIdNumber.Trim();
        }

        // 4. Guardar impresora nueva si hace falta.
        if (printer == null)
        {
            printer = new Printer
            {
                Customer = customer,
                Brand = form.Brand!.Trim(),
                Model = form.Model!.Trim(),
                SerialNumber = string.IsNullOrEmpty(serial) ? null : serial,
                Technology = form.Technology,
                Category = form.Category,
                IsMultifunction = form.IsMultifunction,
                IsColor = form.IsColor,
                Description = form.PrinterDescription?.Trim()
            };
            _db.Printers.Add(printer);
        }

        // 5. La orden.
        var now = DateTime.UtcNow;
        var order = new RepairOrder
        {
            Printer = printer,
            Status = OrderStatus.Received,
            Accessories = JoinAccessories(form),
            PhysicalCondition = form.PhysicalCondition?.Trim(),
            ReportedIssue = form.ReportedIssue.Trim(),
            Quote = form.Quote,
            Deposit = form.Deposit ?? 0,
            EstimatedDate = form.EstimatedDate,
            ReceivedAt = now,
            UpdatedAt = now
        };
        _db.RepairOrders.Add(order);

        await _db.SaveChangesAsync();

        TempData["Message"] = $"Orden {order.Number} creada. Ya podés imprimir la etiqueta y la hoja.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    // ---------- Ficha completa ----------
    public async Task<IActionResult> Details(int id)
    {
        var order = await LoadOrder(id);
        return order == null ? NotFound() : View(order);
    }

    // ---------- Cambiar estado ----------
    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus status)
    {
        var order = await _db.RepairOrders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            return NotFound();
        }

        if (!Enum.IsDefined(status))
        {
            return BadRequest();
        }

        var now = DateTime.UtcNow;

        // "Lista desde": cuándo quedó esperando a que la retiren (reparada o no).
        if (RepairOrder.IsAwaitingPickupStatus(status))
        {
            order.ReadyAt ??= now;
        }
        else if (!RepairOrder.IsClosedStatus(status))
        {
            order.ReadyAt = null; // volvió a trabajo
        }

        // Fecha de entrega: solo si el cliente se la llevó.
        order.DeliveredAt = status is OrderStatus.Delivered or OrderStatus.ReturnedUnrepaired
            ? order.DeliveredAt ?? now
            : null;

        // Garantía: se pone sola al entregar una reparada (si no la habían escrito a mano).
        if (status == OrderStatus.Delivered && order.WarrantyDays == null && _shop.WarrantyDays > 0)
        {
            order.WarrantyDays = _shop.WarrantyDays;
        }

        order.Status = status;
        order.UpdatedAt = now;
        await _db.SaveChangesAsync();

        TempData["Message"] = $"Estado cambiado a \"{status.Name()}\".";
        TempData["SuggestWhatsApp"] = true;
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Editar diagnóstico, cobro, fechas ----------
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await LoadOrder(id);
        if (order == null)
        {
            return NotFound();
        }

        return View(new OrderEditViewModel
        {
            Id = order.Id,
            Accessories = order.Accessories,
            PhysicalCondition = order.PhysicalCondition,
            ReportedIssue = order.ReportedIssue,
            Diagnosis = order.Diagnosis,
            PendingWork = order.PendingWork,
            Quote = order.Quote,
            Deposit = order.Deposit,
            EstimatedDate = order.EstimatedDate,
            WarrantyDays = order.WarrantyDays,
            Order = order
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, OrderEditViewModel model)
    {
        var order = await LoadOrder(id);
        if (order == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.Order = order;
            return View(model);
        }

        order.Accessories = model.Accessories?.Trim();
        order.PhysicalCondition = model.PhysicalCondition?.Trim();
        order.ReportedIssue = model.ReportedIssue.Trim();
        order.Diagnosis = model.Diagnosis?.Trim();
        order.PendingWork = model.PendingWork?.Trim();
        order.Quote = model.Quote;
        order.Deposit = model.Deposit;
        order.EstimatedDate = model.EstimatedDate;
        order.WarrantyDays = model.WarrantyDays;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Cambios guardados.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Para imprimir ----------
    // Etiqueta adhesiva 4 × 6 con QR para pegar en la impresora.
    public async Task<IActionResult> Label(int id)
    {
        var order = await LoadOrder(id);
        return order == null ? NotFound() : View(order);
    }

    // Boleta de ingreso en papel normal: la copia que se lleva el cliente.
    public async Task<IActionResult> Sheet(int id)
    {
        var order = await LoadOrder(id);
        return order == null ? NotFound() : View(order);
    }

    // ---------- Respaldo en Excel ----------
    public async Task<IActionResult> Export()
    {
        var orders = await _db.RepairOrders.AsNoTracking()
            .Include(o => o.Printer).ThenInclude(p => p!.Customer)
            .OrderBy(o => o.Id)
            .ToListAsync();

        var printers = await _db.Printers.AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Orders)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var customers = await _db.Customers.AsNoTracking()
            .Include(c => c.Printers)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var excel = new XlsxWriter();

        excel.AddSheet("Órdenes",
            new[]
            {
                "Orden", "Estado", "Ingreso", "Cliente", "Teléfono", "Correo", "Marca", "Modelo", "Serie",
                "Tecnología", "Tipo", "Accesorios", "Estado físico", "Falla reportada", "Diagnóstico",
                "Pendientes", "Diagnóstico", "Reparación", "Adelanto", "Saldo", "Fecha estimada", "Lista desde",
                "Entregada", "Garantía (días)"
            },
            orders.Select(o => new object?[]
            {
                o.Number, o.Status.Name(), Display.DateTimeShort(o.ReceivedAt),
                o.Printer?.Customer?.Name, o.Printer?.Customer?.Phone, o.Printer?.Customer?.Email,
                o.Printer?.Brand, o.Printer?.Model, o.Printer?.SerialNumber,
                o.Printer?.Technology.Name(), o.Printer?.Category.Name(),
                o.Accessories, o.PhysicalCondition, o.ReportedIssue, o.Diagnosis, o.PendingWork,
                _shop.DiagnosisFee, o.ChargesDiagnosisOnly ? null : o.Quote, o.Deposit,
                o.HasFinalPrice ? o.AmountDue(_shop.DiagnosisFee) : null,
                o.EstimatedDate.HasValue ? Display.Date(o.EstimatedDate) : null,
                o.ReadyAt.HasValue ? Display.DateTimeShort(o.ReadyAt) : null,
                o.DeliveredAt.HasValue ? Display.DateTimeShort(o.DeliveredAt) : null,
                o.WarrantyDays
            }));

        excel.AddSheet("Equipos",
            new[] { "Id", "Cliente", "Teléfono", "Marca", "Modelo", "Serie", "Tecnología", "Tipo", "Multifuncional", "Color", "Detalles", "Veces en el taller" },
            printers.Select(p => new object?[]
            {
                p.Id, p.Customer?.Name, p.Customer?.Phone, p.Brand, p.Model, p.SerialNumber,
                p.Technology.Name(), p.Category.Name(), p.IsMultifunction, p.IsColor, p.Description, p.Orders.Count
            }));

        excel.AddSheet("Clientes",
            new[] { "Id", "Nombre", "Teléfono", "Correo", "Cédula", "Notas", "Equipos", "Registrado" },
            customers.Select(c => new object?[]
            {
                c.Id, c.Name, c.Phone, c.Email, c.IdNumber, c.Notes, c.Printers.Count, Display.Date(c.CreatedAt)
            }));

        var fileName = $"GutierrezPrinters_respaldo_{Display.Today:yyyy-MM-dd}.xlsx";
        return File(excel.Build(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ---------- Ayudantes ----------
    private Task<RepairOrder?> LoadOrder(int id) =>
        _db.RepairOrders
            .Include(o => o.Printer).ThenInclude(p => p!.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);

    private Task<List<Printer>> PrintersOf(int customerId) =>
        _db.Printers.AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

    private static void FillCustomer(NewOrderViewModel model, Customer customer)
    {
        model.CustomerId = customer.Id;
        model.CustomerName = customer.Name;
        model.CustomerPhone = customer.Phone;
        model.CustomerEmail = customer.Email;
        model.CustomerIdNumber = customer.IdNumber;
    }

    private static string? JoinAccessories(NewOrderViewModel model)
    {
        var items = model.SelectedAccessories
            .Where(a => NewOrderViewModel.AccessoryOptions.Contains(a))
            .ToList();

        if (!string.IsNullOrWhiteSpace(model.OtherAccessories))
        {
            items.Add(model.OtherAccessories.Trim());
        }

        return items.Count == 0 ? null : string.Join(", ", items);
    }
}

using GutierrezPrinters.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GutierrezPrinters.Web.Data;

// El "traductor" entre C# y la base de datos.
// Cada DbSet es una tabla. IdentityDbContext agrega solas las tablas del login (usuarios, roles...).
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    // Todas las tablas van en un esquema propio llamado "taller" (no en "public").
    // Así la API automática de Supabase no las expone.
    public const string Schema = "taller";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<RepairOrder> RepairOrders => Set<RepairOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Primero lo de Identity (tablas del login).
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        // Un cliente tiene muchas impresoras. "Restrict" = no deja borrar un cliente que tiene impresoras.
        modelBuilder.Entity<Printer>()
            .HasOne(p => p.Customer)
            .WithMany(c => c.Printers)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una impresora tiene muchas órdenes (cada vez que entra al taller).
        modelBuilder.Entity<RepairOrder>()
            .HasOne(o => o.Printer)
            .WithMany(p => p.Orders)
            .HasForeignKey(o => o.PrinterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Los enums se guardan como texto ("Ready", "Laser"...) para que se lean fácil en SQL.
        modelBuilder.Entity<RepairOrder>().Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<Printer>().Property(p => p.Technology).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<Printer>().Property(p => p.Category).HasConversion<string>().HasMaxLength(30);

        // Índices para que el buscador sea rápido.
        modelBuilder.Entity<Customer>().HasIndex(c => c.Phone);
        modelBuilder.Entity<Printer>().HasIndex(p => p.SerialNumber);
        modelBuilder.Entity<RepairOrder>().HasIndex(o => o.Status);
    }
}

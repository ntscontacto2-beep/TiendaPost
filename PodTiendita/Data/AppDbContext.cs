using Microsoft.EntityFrameworkCore;
using PodTiendita.Models;

namespace PodTiendita.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
        public DbSet<Movimiento> Movimientos => Set<Movimiento>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Usuario>(e =>
            {
                e.HasIndex(u => u.NombreUsuario).IsUnique();
                e.Property(u => u.NombreUsuario).HasMaxLength(50).IsRequired();
                e.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
                e.Property(u => u.PasswordHash).IsRequired();
                e.Ignore(u => u.EsAdministrador);
            });

            b.Entity<Producto>(e =>
            {
                e.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
                e.Property(p => p.Descripcion).HasMaxLength(250);
                e.Property(p => p.ImagenRuta).HasMaxLength(260);
                e.Property(p => p.Precio).HasPrecision(10, 2);
                e.Ignore(p => p.StockBajo);
                e.Ignore(p => p.SinStock);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Producto_Stock", "Stock >= 0");
                    t.HasCheckConstraint("CK_Producto_Precio", "Precio >= 0");
                });
            });

            b.Entity<Venta>(e =>
            {
                e.Property(v => v.Total).HasPrecision(10, 2);
                e.Property(v => v.DescuentoPorcentaje).HasPrecision(5, 2);
                e.Property(v => v.Estado).HasDefaultValue(EstadoVenta.Pagada);
                e.HasIndex(v => v.Fecha);
                e.HasOne(v => v.Usuario).WithMany().HasForeignKey(v => v.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(v => v.Detalles).WithOne().HasForeignKey(d => d.VentaId).OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<DetalleVenta>(e =>
            {
                e.Property(d => d.PrecioUnitario).HasPrecision(10, 2);
                e.Ignore(d => d.Subtotal);
                e.HasOne(d => d.Producto).WithMany().HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<Movimiento>(e =>
            {
                e.Property(m => m.ProductoNombre).HasMaxLength(100);
                e.Property(m => m.Detalle).HasMaxLength(500);
                e.HasIndex(m => m.FechaHora);
                e.HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

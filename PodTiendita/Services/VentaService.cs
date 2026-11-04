using Microsoft.EntityFrameworkCore;
using PodTiendita.Data;
using PodTiendita.Models;

namespace PodTiendita.Services
{
    public record LineaVenta(int ProductoId, int Cantidad);

    public class VentaService
    {
        /// <summary>
        /// Registra la venta, descuenta el stock y deja el movimiento en la bitácora.
        /// Todo se guarda en una sola transacción: o se guarda completo o no se guarda nada.
        /// El precio y el stock se leen de la BD, nunca de lo que mande la pantalla.
        /// </summary>
        public Venta Registrar(int usuarioId, IReadOnlyList<LineaVenta> lineas, MetodoPago metodoPago, decimal descuentoPorcentaje)
        {
            if (lineas.Count == 0)
                throw new NegocioException("La venta no tiene productos.");
            if (descuentoPorcentaje is < 0 or > 100)
                throw new NegocioException("El descuento debe estar entre 0 y 100%.");

            using var db = new AppDbContext(App.DbOptions);
            using var tx = db.Database.BeginTransaction();

            var ids = lineas.Select(l => l.ProductoId).ToList();
            var productos = db.Productos.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id);

            var venta = new Venta { UsuarioId = usuarioId, MetodoPago = metodoPago, DescuentoPorcentaje = descuentoPorcentaje };

            foreach (var linea in lineas)
            {
                if (linea.Cantidad <= 0)
                    throw new NegocioException("La cantidad debe ser mayor a cero.");
                if (!productos.TryGetValue(linea.ProductoId, out var producto) || !producto.Activo)
                    throw new NegocioException("Uno de los productos ya no está disponible.");
                if (producto.Stock < linea.Cantidad)
                    throw new NegocioException($"Stock insuficiente de \"{producto.Nombre}\": quedan {producto.Stock}.");

                var anterior = producto.Stock;
                producto.Stock -= linea.Cantidad;

                venta.Detalles.Add(new DetalleVenta
                {
                    ProductoId = producto.Id,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = producto.Precio,
                });

                db.Movimientos.Add(new Movimiento
                {
                    Tipo = TipoMovimiento.Venta,
                    UsuarioId = usuarioId,
                    ProductoId = producto.Id,
                    ProductoNombre = producto.Nombre,
                    StockAnterior = anterior,
                    StockNuevo = producto.Stock,
                    Detalle = $"Venta de {linea.Cantidad} pza(s) a {producto.Precio:C2} c/u",
                });
            }

            var subtotal = venta.Detalles.Sum(d => d.Subtotal);
            venta.Total = Math.Round(subtotal * (1 - descuentoPorcentaje / 100m), 2);

            db.Ventas.Add(venta);
            db.SaveChanges();
            tx.Commit();
            return venta;
        }

        /// <summary>
        /// Cancela una venta: la marca como cancelada, regresa el stock y lo deja en la bitácora.
        /// Solo el administrador puede hacerlo (se valida contra la BD, no contra la pantalla).
        /// </summary>
        public void Cancelar(int ventaId, int usuarioId)
        {
            using var db = new AppDbContext(App.DbOptions);
            using var tx = db.Database.BeginTransaction();

            Permisos.ExigirAdministrador(db, usuarioId, "cancelar ventas");

            var venta = db.Ventas.Include(v => v.Detalles).FirstOrDefault(v => v.Id == ventaId)
                ?? throw new NegocioException("La venta no existe.");
            if (venta.Estado == EstadoVenta.Cancelada)
                throw new NegocioException("La venta ya estaba cancelada.");

            foreach (var detalle in venta.Detalles)
            {
                var producto = db.Productos.Find(detalle.ProductoId)!;
                var anterior = producto.Stock;
                producto.Stock += detalle.Cantidad;

                db.Movimientos.Add(new Movimiento
                {
                    Tipo = TipoMovimiento.CancelacionVenta,
                    UsuarioId = usuarioId,
                    ProductoId = producto.Id,
                    ProductoNombre = producto.Nombre,
                    StockAnterior = anterior,
                    StockNuevo = producto.Stock,
                    Detalle = $"Cancelación de la venta #{venta.Id:D4}: regresan {detalle.Cantidad} pza(s)",
                });
            }

            venta.Estado = EstadoVenta.Cancelada;
            db.SaveChanges();
            tx.Commit();
        }
    }
}

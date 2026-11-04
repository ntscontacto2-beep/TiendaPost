using Microsoft.EntityFrameworkCore;
using PodTiendita.Data;
using PodTiendita.Models;

namespace PodTiendita.Services
{
    public record RebanadaPastel(string Etiqueta, decimal Monto, decimal Porcentaje, string ColorHex);

    public class ResumenVentas
    {
        public DateTime Desde { get; init; }
        public DateTime Hasta { get; init; }
        public List<Venta> Ventas { get; init; } = new();

        // Las canceladas se listan, pero no cuentan en los totales.
        public List<Venta> Pagadas => Ventas.Where(v => v.Estado == EstadoVenta.Pagada).ToList();
        public int NumVentas => Pagadas.Count;
        public decimal Total => Pagadas.Sum(v => v.Total);
        public decimal TicketPromedio => NumVentas == 0 ? 0 : Math.Round(Total / NumVentas, 2);
        public List<RebanadaPastel> Rebanadas { get; init; } = new();
    }

    public partial class ReporteService
    {
        // Paleta sobria (azules y grises) acorde a la app. Se comparte entre la pantalla y el PDF.
        public static readonly string[] Paleta =
            { "#1F4E79", "#5B7B9A", "#9DB2C8", "#B45309", "#4B5563", "#D1D7DE" };

        private const int MaxRebanadas = 5;

        /// <summary>Ventas entre [desde, hasta) con los filtros opcionales.</summary>
        public ResumenVentas Consultar(DateTime desde, DateTime hasta, MetodoPago? metodo = null,
            EstadoVenta? estado = null, string? texto = null)
        {
            using var db = new AppDbContext(App.DbOptions);

            var consulta = db.Ventas.AsNoTracking()
                .Include(v => v.Usuario)
                .Include(v => v.Detalles).ThenInclude(d => d.Producto)
                .Where(v => v.Fecha >= desde && v.Fecha < hasta);

            if (metodo is not null) consulta = consulta.Where(v => v.MetodoPago == metodo);
            if (estado is not null) consulta = consulta.Where(v => v.Estado == estado);

            var ventas = consulta.OrderByDescending(v => v.Fecha).ToList();

            // Búsqueda por folio o por cajero (en memoria: son pocas filas por periodo).
            var buscado = texto?.Trim().TrimStart('#');
            if (!string.IsNullOrEmpty(buscado))
                ventas = ventas.Where(v =>
                    v.Id.ToString().Contains(buscado) ||
                    (v.Usuario?.Nombre.Contains(buscado, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToList();

            var resumen = new ResumenVentas { Desde = desde, Hasta = hasta, Ventas = ventas };
            return new ResumenVentas
            {
                Desde = desde,
                Hasta = hasta,
                Ventas = ventas,
                Rebanadas = CalcularRebanadas(resumen.Pagadas),
            };
        }

        /// <summary>Ventas por producto: los más vendidos y el resto agrupado en "Otros".</summary>
        private static List<RebanadaPastel> CalcularRebanadas(List<Venta> pagadas)
        {
            // El descuento se reparte proporcional para que las rebanadas sumen el total cobrado.
            var porProducto = pagadas
                .SelectMany(v => v.Detalles.Select(d => (
                    Nombre: d.Producto?.Nombre ?? "Producto",
                    Monto: d.Subtotal * (1 - v.DescuentoPorcentaje / 100m))))
                .GroupBy(x => x.Nombre)
                .Select(g => (Nombre: g.Key, Monto: g.Sum(x => x.Monto)))
                .OrderByDescending(x => x.Monto)
                .ToList();

            var total = porProducto.Sum(x => x.Monto);
            if (total <= 0) return new();

            var visibles = porProducto.Take(MaxRebanadas).ToList();
            var resto = porProducto.Skip(MaxRebanadas).Sum(x => x.Monto);
            if (resto > 0) visibles.Add(("Otros", resto));

            return visibles
                .Select((x, i) => new RebanadaPastel(
                    x.Nombre,
                    Math.Round(x.Monto, 2),
                    Math.Round(x.Monto / total * 100m, 1),
                    Paleta[i % Paleta.Length]))
                .ToList();
        }
    }
}

using System.Globalization;
using PodTiendita.Models;

namespace PodTiendita.Services
{
    public enum TipoPeriodo
    {
        Semana,
        Mes,
        Trimestre,
    }

    public record BarraVentas(string Etiqueta, decimal Monto, int NumVentas);
    public record ProductoVendido(string Nombre, string? ImagenRuta, int Unidades, decimal Monto, decimal Porcentaje);
    public record VentaCategoria(CategoriaProducto Categoria, decimal Monto, decimal Porcentaje);
    public record VentaMetodo(MetodoPago Metodo, decimal Monto, decimal Porcentaje);

    /// <summary>Resumen de lo vendido en una semana, un mes o un trimestre.</summary>
    public class ReportePeriodo
    {
        public TipoPeriodo Tipo { get; init; }
        public DateTime Desde { get; init; }
        /// <summary>Límite superior exclusivo.</summary>
        public DateTime Hasta { get; init; }
        public string Etiqueta { get; init; } = "";

        public int NumVentas { get; init; }
        public decimal Total { get; init; }
        public decimal TicketPromedio { get; init; }
        public int Canceladas { get; init; }

        public List<BarraVentas> Barras { get; init; } = new();
        public List<VentaMetodo> Metodos { get; init; } = new();
        public List<VentaCategoria> Categorias { get; init; } = new();
        public List<ProductoVendido> Productos { get; init; } = new();

        // Periodo anterior, medido sobre el mismo tramo transcurrido (para comparar parejo).
        public int NumVentasAnterior { get; init; }
        public decimal TotalAnterior { get; init; }
        public decimal TicketAnterior { get; init; }

        public bool PorSemana => Tipo == TipoPeriodo.Trimestre;
        public string UnidadBarras => PorSemana ? "semana" : "día";

        public decimal? VariacionTotal => Variacion(Total, TotalAnterior);
        public decimal? VariacionVentas => Variacion(NumVentas, NumVentasAnterior);
        public decimal? VariacionTicket => Variacion(TicketPromedio, TicketAnterior);

        private static decimal? Variacion(decimal actual, decimal anterior) =>
            anterior > 0 ? Math.Round((actual - anterior) / anterior * 100m, 1) : null;
    }

    public partial class ReporteService
    {
        private static readonly CultureInfo Mx = new("es-MX");

        /// <summary>Rango [desde, hasta) del periodo. desplazamiento 0 = el actual, -1 = el anterior, etc.</summary>
        public static (DateTime Desde, DateTime Hasta) Rango(TipoPeriodo tipo, int desplazamiento, DateTime? hoy = null)
        {
            var dia = (hoy ?? DateTime.Today).Date;
            switch (tipo)
            {
                case TipoPeriodo.Semana:
                    // La semana empieza en lunes.
                    var lunes = dia.AddDays(-(((int)dia.DayOfWeek + 6) % 7)).AddDays(7 * desplazamiento);
                    return (lunes, lunes.AddDays(7));
                case TipoPeriodo.Mes:
                    var primero = new DateTime(dia.Year, dia.Month, 1).AddMonths(desplazamiento);
                    return (primero, primero.AddMonths(1));
                default:
                    // Trimestre calendario: ene–mar, abr–jun, jul–sep, oct–dic.
                    var inicio = new DateTime(dia.Year, (dia.Month - 1) / 3 * 3 + 1, 1).AddMonths(3 * desplazamiento);
                    return (inicio, inicio.AddMonths(3));
            }
        }

        public static string EtiquetaPeriodo(TipoPeriodo tipo, DateTime desde, DateTime hasta)
        {
            var ultimo = hasta.AddDays(-1);
            switch (tipo)
            {
                case TipoPeriodo.Semana:
                    return desde.Month == ultimo.Month
                        ? $"{desde.Day} al {ultimo.Day} de {Mx.DateTimeFormat.GetMonthName(ultimo.Month)} de {ultimo.Year}"
                        : $"{desde.Day} de {Mx.DateTimeFormat.GetAbbreviatedMonthName(desde.Month)} al {ultimo.Day} de {Mx.DateTimeFormat.GetAbbreviatedMonthName(ultimo.Month)} de {ultimo.Year}";
                case TipoPeriodo.Mes:
                    return Mayuscula($"{Mx.DateTimeFormat.GetMonthName(desde.Month)} {desde.Year}");
                default:
                    return $"T{(desde.Month - 1) / 3 + 1} {desde.Year} · {Mx.DateTimeFormat.GetMonthName(desde.Month)} – {Mx.DateTimeFormat.GetMonthName(ultimo.Month)}";
            }
        }

        public ReportePeriodo ObtenerReporte(TipoPeriodo tipo, int desplazamiento = 0)
        {
            var (desde, hasta) = Rango(tipo, desplazamiento);
            var pagadas = Consultar(desde, hasta).Pagadas;
            var canceladas = Consultar(desde, hasta, estado: EstadoVenta.Cancelada).Ventas.Count;

            var total = pagadas.Sum(v => v.Total);
            var ticket = pagadas.Count == 0 ? 0 : Math.Round(total / pagadas.Count, 2);

            // Periodo anterior: si el actual va a medias, se compara solo el mismo tramo.
            var (anteriorDesde, anteriorHasta) = Rango(tipo, desplazamiento - 1);
            var finComparable = desplazamiento == 0
                ? Min(anteriorHasta, anteriorDesde + (DateTime.Now - desde))
                : anteriorHasta;
            var anteriores = Consultar(anteriorDesde, finComparable).Pagadas;
            var totalAnterior = anteriores.Sum(v => v.Total);

            // Los renglones con descuento se prorratean para que los desgloses sumen lo realmente cobrado.
            var renglones = pagadas
                .SelectMany(v => v.Detalles.Select(d => (
                    Producto: d.Producto,
                    Cantidad: d.Cantidad,
                    Monto: d.Subtotal * (1 - v.DescuentoPorcentaje / 100m))))
                .ToList();

            return new ReportePeriodo
            {
                Tipo = tipo,
                Desde = desde,
                Hasta = hasta,
                Etiqueta = EtiquetaPeriodo(tipo, desde, hasta),
                NumVentas = pagadas.Count,
                Total = total,
                TicketPromedio = ticket,
                Canceladas = canceladas,
                Barras = ArmarBarras(tipo, desde, hasta, pagadas),
                Metodos = pagadas
                    .GroupBy(v => v.MetodoPago)
                    .Select(g => new VentaMetodo(g.Key, g.Sum(v => v.Total), Porcentaje(g.Sum(v => v.Total), total)))
                    .OrderByDescending(m => m.Monto)
                    .ToList(),
                Categorias = renglones
                    .Where(r => r.Producto is not null)
                    .GroupBy(r => r.Producto!.Categoria)
                    .Select(g => new VentaCategoria(g.Key, g.Sum(r => r.Monto), Porcentaje(g.Sum(r => r.Monto), total)))
                    .OrderByDescending(c => c.Monto)
                    .ToList(),
                Productos = renglones
                    .GroupBy(r => r.Producto?.Id ?? 0)
                    .Select(g => new ProductoVendido(
                        g.First().Producto?.Nombre ?? "Producto",
                        g.First().Producto?.ImagenRuta,
                        g.Sum(r => r.Cantidad),
                        Math.Round(g.Sum(r => r.Monto), 2),
                        Porcentaje(g.Sum(r => r.Monto), total)))
                    .OrderByDescending(p => p.Monto)
                    .ToList(),
                NumVentasAnterior = anteriores.Count,
                TotalAnterior = totalAnterior,
                TicketAnterior = anteriores.Count == 0 ? 0 : Math.Round(totalAnterior / anteriores.Count, 2),
            };
        }

        /// <summary>Semana y mes: una barra por día. Trimestre: una barra por semana (si no serían ~90 barras).</summary>
        private static List<BarraVentas> ArmarBarras(TipoPeriodo tipo, DateTime desde, DateTime hasta, List<Venta> pagadas)
        {
            var barras = new List<BarraVentas>();

            if (tipo != TipoPeriodo.Trimestre)
            {
                var porDia = pagadas.GroupBy(v => v.Fecha.Date).ToDictionary(g => g.Key, g => g.ToList());
                for (var dia = desde; dia < hasta; dia = dia.AddDays(1))
                {
                    porDia.TryGetValue(dia, out var ventas);
                    var etiqueta = tipo == TipoPeriodo.Semana
                        ? Mayuscula(Mx.DateTimeFormat.GetAbbreviatedDayName(dia.DayOfWeek))
                        : dia.Day.ToString();
                    barras.Add(new BarraVentas(etiqueta, ventas?.Sum(v => v.Total) ?? 0, ventas?.Count ?? 0));
                }
                return barras;
            }

            // Cada semana (lunes a domingo) recortada al trimestre.
            var inicio = desde;
            while (inicio < hasta)
            {
                var diasHastaLunes = (8 - (int)inicio.DayOfWeek) % 7;
                var fin = Min(hasta, inicio.AddDays(diasHastaLunes == 0 ? 7 : diasHastaLunes));
                var ventas = pagadas.Where(v => v.Fecha >= inicio && v.Fecha < fin).ToList();
                barras.Add(new BarraVentas(
                    $"{inicio.Day} {Mx.DateTimeFormat.GetAbbreviatedMonthName(inicio.Month).TrimEnd('.')}",
                    ventas.Sum(v => v.Total), ventas.Count));
                inicio = fin;
            }
            return barras;
        }

        private static decimal Porcentaje(decimal parte, decimal total) =>
            total <= 0 ? 0 : Math.Round(parte / total * 100m, 1);

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        private static string Mayuscula(string texto) =>
            texto.Length == 0 ? texto : char.ToUpper(texto[0], Mx) + texto[1..];
    }
}

using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PodTiendita.Models;

namespace PodTiendita.Services
{
    /// <summary>Genera el reporte de ventas en PDF (resumen, gráfica de pastel y detalle).</summary>
    public partial class PdfService
    {
        private static readonly CultureInfo Cultura = new("es-MX");

        private const double Margen = 40;
        private const double AnchoPagina = 595;   // A4 en puntos
        private const double AltoPagina = 842;
        private const double LimiteInferior = AltoPagina - 50;

        private static readonly XColor Acento = XColor.FromArgb(0x1F, 0x4E, 0x79);
        private static readonly XColor Oscuro = XColor.FromArgb(0x1F, 0x29, 0x37);
        private static readonly XColor Gris = XColor.FromArgb(0x6B, 0x72, 0x80);
        private static readonly XColor Linea = XColor.FromArgb(0xD9, 0xDE, 0xE4);
        private static readonly XColor Rojo = XColor.FromArgb(0xB4, 0x23, 0x18);

        private readonly XFont _titulo = new("Segoe UI", 22, XFontStyleEx.Bold);
        private readonly XFont _subtitulo = new("Segoe UI", 12, XFontStyleEx.Regular);
        private readonly XFont _seccion = new("Segoe UI", 13, XFontStyleEx.Bold);
        private readonly XFont _normal = new("Segoe UI", 9.5, XFontStyleEx.Regular);
        private readonly XFont _negrita = new("Segoe UI", 9.5, XFontStyleEx.Bold);
        private readonly XFont _kpi = new("Segoe UI", 18, XFontStyleEx.Bold);
        private readonly XFont _chico = new("Segoe UI", 8.5, XFontStyleEx.Regular);

        public void Generar(ResumenVentas resumen, string titulo, string ruta)
        {
            using var doc = new PdfDocument();
            doc.Info.Title = titulo;

            var (gfx, y) = NuevaPagina(doc);
            y = Encabezado(gfx, titulo, y);
            y = Indicadores(gfx, resumen, y);
            y = Pastel(gfx, resumen, y);

            // Tabla de ventas: salta de página cuando ya no cabe.
            gfx.DrawString("Detalle de ventas", _seccion, new XSolidBrush(Oscuro), Margen, y + 12);
            y += 28;
            y = EncabezadoTabla(gfx, y);

            if (resumen.Ventas.Count == 0)
                gfx.DrawString("No hay ventas en este periodo.", _normal, new XSolidBrush(Gris), Margen, y + 14);

            foreach (var venta in resumen.Ventas)
            {
                if (y > LimiteInferior)
                {
                    gfx.Dispose();
                    (gfx, y) = NuevaPagina(doc);
                    y = EncabezadoTabla(gfx, y);
                }
                y = FilaVenta(gfx, venta, y);
            }

            gfx.Dispose();
            doc.Save(ruta);
        }

        private static (XGraphics, double) NuevaPagina(PdfDocument doc)
        {
            var pagina = doc.AddPage();
            pagina.Width = XUnit.FromPoint(AnchoPagina);
            pagina.Height = XUnit.FromPoint(AltoPagina);
            return (XGraphics.FromPdfPage(pagina), Margen);
        }

        private double Encabezado(XGraphics gfx, string titulo, double y)
        {
            gfx.DrawString("PodTiendita", _titulo, new XSolidBrush(Acento), Margen, y + 22);
            gfx.DrawString(titulo, _subtitulo, new XSolidBrush(Oscuro), Margen, y + 42);
            var generado = $"Generado: {DateTime.Now.ToString("d MMM yyyy, HH:mm", Cultura)}";
            gfx.DrawString(generado, _chico, new XSolidBrush(Gris),
                new XRect(Margen, y + 8, AnchoPagina - 2 * Margen, 14), XStringFormats.TopRight);
            gfx.DrawLine(new XPen(Linea, 1), Margen, y + 54, AnchoPagina - Margen, y + 54);
            return y + 70;
        }

        private double Indicadores(XGraphics gfx, ResumenVentas r, double y)
        {
            var ancho = (AnchoPagina - 2 * Margen - 24) / 3;
            var datos = new[]
            {
                ("Ventas realizadas", r.NumVentas.ToString()),
                ("Total vendido (MXN)", r.Total.ToString("C2", Cultura)),
                ("Ticket promedio (MXN)", r.TicketPromedio.ToString("C2", Cultura)),
            };

            for (var i = 0; i < datos.Length; i++)
            {
                var x = Margen + i * (ancho + 12);
                var caja = new XRect(x, y, ancho, 58);
                gfx.DrawRectangle(new XPen(Linea, 1), XBrushes.White, caja);
                gfx.DrawString(datos[i].Item1, _chico, new XSolidBrush(Gris), x + 14, y + 20);
                gfx.DrawString(datos[i].Item2, _kpi, new XSolidBrush(Oscuro), x + 14, y + 44);
            }
            return y + 78;
        }

        private double Pastel(XGraphics gfx, ResumenVentas r, double y)
        {
            gfx.DrawString("Ventas por producto", _seccion, new XSolidBrush(Oscuro), Margen, y + 12);
            y += 26;

            if (r.Rebanadas.Count == 0)
            {
                gfx.DrawString("Sin ventas pagadas en este periodo.", _normal, new XSolidBrush(Gris), Margen, y + 14);
                return y + 36;
            }

            const double diametro = 150;
            var area = new XRect(Margen + 10, y, diametro, diametro);
            var inicio = -90.0;

            foreach (var rebanada in r.Rebanadas)
            {
                var color = ColorDe(rebanada.ColorHex);
                var barrido = (double)rebanada.Porcentaje / 100.0 * 360.0;
                if (r.Rebanadas.Count == 1)
                    gfx.DrawEllipse(new XSolidBrush(color), area);
                else
                    gfx.DrawPie(new XPen(XColors.White, 1.5), new XSolidBrush(color), area, inicio, barrido);
                inicio += barrido;
            }

            // Leyenda a la derecha de la gráfica.
            var lx = Margen + diametro + 50;
            var ly = y + 10;
            foreach (var rebanada in r.Rebanadas)
            {
                gfx.DrawRectangle(new XSolidBrush(ColorDe(rebanada.ColorHex)), new XRect(lx, ly - 8, 10, 10));
                gfx.DrawString(rebanada.Etiqueta, _normal, new XSolidBrush(Oscuro), lx + 18, ly);
                gfx.DrawString($"{rebanada.Monto.ToString("C2", Cultura)}  ·  {rebanada.Porcentaje:0.#}%", _chico,
                    new XSolidBrush(Gris), lx + 18, ly + 12);
                ly += 28;
            }

            // La leyenda puede ser más alta que la gráfica.
            return Math.Max(y + diametro, ly) + 16;
        }

        private double EncabezadoTabla(XGraphics gfx, double y)
        {
            var gris = new XSolidBrush(Gris);
            gfx.DrawString("FOLIO", _chico, gris, Columnas[0], y + 10);
            gfx.DrawString("FECHA Y HORA", _chico, gris, Columnas[1], y + 10);
            gfx.DrawString("CAJERO", _chico, gris, Columnas[2], y + 10);
            gfx.DrawString("MÉTODO", _chico, gris, Columnas[3], y + 10);
            gfx.DrawString("ESTADO", _chico, gris, Columnas[4], y + 10);
            gfx.DrawString("TOTAL", _chico, gris, new XRect(Columnas[5], y, ColumnaTotalAncho, 14), XStringFormats.TopRight);
            gfx.DrawLine(new XPen(Linea, 1), Margen, y + 16, AnchoPagina - Margen, y + 16);
            return y + 16;
        }

        // Posiciones X de cada columna de la tabla.
        private static readonly double[] Columnas = { Margen, Margen + 55, Margen + 160, Margen + 290, Margen + 360, Margen + 420 };
        private const double ColumnaTotalAncho = AnchoPagina - Margen - (Margen + 420);

        private double FilaVenta(XGraphics gfx, Venta v, double y)
        {
            var cancelada = v.Estado == EstadoVenta.Cancelada;
            var brush = new XSolidBrush(cancelada ? Gris : Oscuro);

            gfx.DrawString($"#{v.Id:D4}", _negrita, brush, Columnas[0], y + 15);
            gfx.DrawString(v.Fecha.ToString("dd/MM/yy HH:mm", Cultura), _normal, brush, Columnas[1], y + 15);
            gfx.DrawString(v.Usuario?.Nombre ?? "-", _normal, brush, Columnas[2], y + 15);
            gfx.DrawString(v.MetodoPago.ToString(), _normal, brush, Columnas[3], y + 15);
            gfx.DrawString(v.Estado.ToString(), _normal, new XSolidBrush(cancelada ? Rojo : Oscuro), Columnas[4], y + 15);
            gfx.DrawString(v.Total.ToString("C2", Cultura), _negrita, brush,
                new XRect(Columnas[5], y + 4, ColumnaTotalAncho, 14), XStringFormats.TopRight);
            gfx.DrawLine(new XPen(Linea, 0.5), Margen, y + 22, AnchoPagina - Margen, y + 22);
            return y + 22;
        }

        private static XColor ColorDe(string hex) =>
            XColor.FromArgb(
                Convert.ToByte(hex.Substring(1, 2), 16),
                Convert.ToByte(hex.Substring(3, 2), 16),
                Convert.ToByte(hex.Substring(5, 2), 16));
    }
}

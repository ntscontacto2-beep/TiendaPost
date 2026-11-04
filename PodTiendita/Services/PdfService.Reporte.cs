using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PodTiendita.Models;

namespace PodTiendita.Services
{
    /// <summary>Reporte semanal, mensual o trimestral: resumen, gráficas y desgloses de lo vendido.</summary>
    public partial class PdfService
    {
        private const double AnchoUtil = AnchoPagina - 2 * Margen;
        private const double AltoFila = 22;

        /// <summary>Hoja en curso: lleva la posición vertical y salta de página cuando algo ya no cabe.</summary>
        private sealed class Lienzo
        {
            private readonly PdfDocument _doc;

            public Lienzo(PdfDocument doc)
            {
                _doc = doc;
                (Gfx, Y) = NuevaPagina(doc);
            }

            public XGraphics Gfx { get; private set; }
            public double Y { get; set; }

            /// <summary>Si no caben <paramref name="alto"/> puntos, abre una página nueva. Devuelve true si la abrió.</summary>
            public bool Asegurar(double alto)
            {
                if (Y + alto <= LimiteInferior) return false;
                Gfx.Dispose();
                (Gfx, Y) = NuevaPagina(_doc);
                return true;
            }
        }

        public void GenerarReporte(ReportePeriodo r, string ruta)
        {
            var tipo = r.Tipo switch { TipoPeriodo.Semana => "semanal", TipoPeriodo.Mes => "mensual", _ => "trimestral" };
            var titulo = $"Reporte {tipo} de ventas · {r.Etiqueta}";

            using var doc = new PdfDocument();
            doc.Info.Title = titulo;

            var l = new Lienzo(doc);
            l.Y = Encabezado(l.Gfx, titulo, l.Y);
            IndicadoresReporte(l, r);
            GraficaBarras(l, r);
            MetodosYCategorias(l, r);
            TablaProductos(l, r);
            TablaPorPeriodo(l, r);

            l.Gfx.Dispose();
            doc.Save(ruta);
        }

        // ----- Indicadores -----

        private void IndicadoresReporte(Lienzo l, ReportePeriodo r)
        {
            var ancho = (AnchoUtil - 3 * 10) / 4;
            var datos = new[]
            {
                ("Ventas totales (MXN)", r.Total.ToString("C2", Cultura), Comparacion(r.VariacionTotal)),
                ("Transacciones", r.NumVentas.ToString(), Comparacion(r.VariacionVentas)),
                ("Ticket promedio (MXN)", r.TicketPromedio.ToString("C2", Cultura), Comparacion(r.VariacionTicket)),
                ("Ventas canceladas", r.Canceladas.ToString(), "No se suman al total"),
            };

            for (var i = 0; i < datos.Length; i++)
            {
                var x = Margen + i * (ancho + 10);
                l.Gfx.DrawRectangle(new XPen(Linea, 1), XBrushes.White, new XRect(x, l.Y, ancho, 66));
                l.Gfx.DrawString(datos[i].Item1, _chico, new XSolidBrush(Gris), x + 10, l.Y + 17);
                l.Gfx.DrawString(datos[i].Item2, new XFont("Segoe UI", 14, XFontStyleEx.Bold), new XSolidBrush(Oscuro), x + 10, l.Y + 39);
                l.Gfx.DrawString(datos[i].Item3, new XFont("Segoe UI", 7.5, XFontStyleEx.Regular), new XSolidBrush(Gris), x + 10, l.Y + 56);
            }
            l.Y += 88;
        }

        private static string Comparacion(decimal? variacion) => variacion is null
            ? "Sin datos del periodo anterior"
            : $"{(variacion >= 0 ? "▲" : "▼")} {Math.Abs(variacion.Value):0.#}% vs. periodo anterior";

        // ----- Gráfica de barras -----

        private void GraficaBarras(Lienzo l, ReportePeriodo r)
        {
            const double alto = 150;
            const double izquierda = 52;

            l.Asegurar(alto + 50);
            var g = l.Gfx;
            g.DrawString($"Ventas por {r.UnidadBarras}", _seccion, new XSolidBrush(Oscuro), Margen, l.Y + 12);

            var area = new XRect(Margen + izquierda, l.Y + 28, AnchoUtil - izquierda, alto);
            var maximoDato = r.Barras.Count == 0 ? 0 : r.Barras.Max(b => b.Monto);
            var maximoEje = EscalaEje.Maximo(maximoDato);

            for (var i = 0; i <= EscalaEje.Divisiones; i++)
            {
                var y = area.Bottom - area.Height * i / EscalaEje.Divisiones;
                g.DrawLine(new XPen(Linea, 0.5), area.Left, y, area.Right, y);
                g.DrawString((maximoEje * i / EscalaEje.Divisiones).ToString("C0", Cultura), _chico, new XSolidBrush(Gris),
                    new XRect(Margen, y - 6, izquierda - 6, 12), XStringFormats.TopRight);
            }

            if (maximoDato <= 0)
                g.DrawString("Sin ventas en este periodo", _normal, new XSolidBrush(Gris),
                    new XRect(area.Left, area.Top + area.Height / 2 - 8, area.Width, 16), XStringFormats.TopCenter);

            if (r.Barras.Count > 0)
            {
                var ranura = area.Width / r.Barras.Count;
                var ancho = Math.Min(34, ranura * 0.6);
                var cadaCuantas = (int)Math.Ceiling(r.Barras.Count / 16.0);

                for (var i = 0; i < r.Barras.Count; i++)
                {
                    var centro = area.Left + ranura * (i + 0.5);
                    var barra = (double)(r.Barras[i].Monto / maximoEje) * area.Height;
                    if (barra > 0)
                        g.DrawRectangle(new XSolidBrush(Acento), centro - ancho / 2, area.Bottom - barra, ancho, barra);
                    if (i % cadaCuantas == 0)
                        g.DrawString(r.Barras[i].Etiqueta, _chico, new XSolidBrush(Gris),
                            new XRect(centro - 20, area.Bottom + 4, 40, 12), XStringFormats.TopCenter);
                }
            }

            l.Y = area.Bottom + 34;
        }

        // ----- Métodos de pago y categorías, lado a lado -----

        private void MetodosYCategorias(Lienzo l, ReportePeriodo r)
        {
            var filas = Math.Max(1, Math.Max(r.Metodos.Count, r.Categorias.Count)); // al menos 1: deja espacio al aviso "sin ventas"
            l.Asegurar(36 + filas * 34);

            const double columna = (AnchoUtil - 30) / 2;
            var segundaX = Margen + columna + 30;
            var oscuro = new XSolidBrush(Oscuro);

            l.Gfx.DrawString("Métodos de pago", _seccion, oscuro, Margen, l.Y + 12);
            l.Gfx.DrawString("Ventas por categoría", _seccion, oscuro, segundaX, l.Y + 12);
            var y = l.Y + 30;

            if (r.Metodos.Count == 0)
                l.Gfx.DrawString("Sin ventas en este periodo.", _normal, new XSolidBrush(Gris), Margen, y + 12);

            for (var i = 0; i < r.Metodos.Count; i++)
                BarraHorizontal(l.Gfx, Margen, y + i * 34, columna, r.Metodos[i].Metodo.ToString(),
                    r.Metodos[i].Monto, r.Metodos[i].Porcentaje, ColorDe(ReporteService.Paleta[i % ReporteService.Paleta.Length]));

            for (var i = 0; i < r.Categorias.Count; i++)
                BarraHorizontal(l.Gfx, segundaX, y + i * 34, columna, r.Categorias[i].Categoria.ToString(),
                    r.Categorias[i].Monto, r.Categorias[i].Porcentaje, ColorDe(ReporteService.Paleta[i % ReporteService.Paleta.Length]));

            l.Y = y + filas * 34 + 14;
        }

        private void BarraHorizontal(XGraphics g, double x, double y, double ancho, string etiqueta, decimal monto, decimal porcentaje, XColor color)
        {
            g.DrawString(etiqueta, _normal, new XSolidBrush(Oscuro), x, y + 10);
            g.DrawString($"{monto.ToString("C2", Cultura)}  ·  {porcentaje:0.#}%", _chico, new XSolidBrush(Gris),
                new XRect(x, y, ancho, 14), XStringFormats.TopRight);
            g.DrawRectangle(new XSolidBrush(Linea), x, y + 16, ancho, 7);
            g.DrawRectangle(new XSolidBrush(color), x, y + 16, ancho * (double)Math.Min(porcentaje, 100) / 100.0, 7);
        }

        // ----- Tablas -----

        private void TablaProductos(Lienzo l, ReportePeriodo r)
        {
            // Posiciones: # | producto | unidades | monto | % del total
            double[] x = { Margen, Margen + 26, Margen + 290, Margen + 360, Margen + 455 };
            double[] ancho = { 20, 255, 60, 90, 60 };

            l.Asegurar(70);
            l.Gfx.DrawString("Productos vendidos", _seccion, new XSolidBrush(Oscuro), Margen, l.Y + 12);
            l.Y += 26;
            EncabezadoColumnas(l, x, ancho, new[] { "#", "PRODUCTO", "UNIDADES", "MONTO", "% DEL TOTAL" }, derecha: 2);

            if (r.Productos.Count == 0)
            {
                l.Gfx.DrawString("No hubo ventas en este periodo.", _normal, new XSolidBrush(Gris), Margen, l.Y + 14);
                l.Y += AltoFila;
            }

            for (var i = 0; i < r.Productos.Count; i++)
            {
                if (l.Asegurar(AltoFila))
                    EncabezadoColumnas(l, x, ancho, new[] { "#", "PRODUCTO", "UNIDADES", "MONTO", "% DEL TOTAL" }, derecha: 2);

                var p = r.Productos[i];
                var g = l.Gfx;
                var negro = new XSolidBrush(Oscuro);
                g.DrawString((i + 1).ToString(), _chico, new XSolidBrush(Gris), x[0], l.Y + 15);
                g.DrawString(Ajustar(g, p.Nombre, _normal, ancho[1]), _normal, negro, x[1], l.Y + 15);
                g.DrawString(p.Unidades.ToString("N0", Cultura), _normal, negro, new XRect(x[2], l.Y + 4, ancho[2], 14), XStringFormats.TopRight);
                g.DrawString(p.Monto.ToString("C2", Cultura), _negrita, negro, new XRect(x[3], l.Y + 4, ancho[3], 14), XStringFormats.TopRight);
                g.DrawString($"{p.Porcentaje:0.#}%", _normal, new XSolidBrush(Gris), new XRect(x[4], l.Y + 4, ancho[4], 14), XStringFormats.TopRight);
                g.DrawLine(new XPen(Linea, 0.5), Margen, l.Y + AltoFila, AnchoPagina - Margen, l.Y + AltoFila);
                l.Y += AltoFila;
            }

            if (r.Productos.Count > 0)
            {
                l.Asegurar(AltoFila);
                var g = l.Gfx;
                g.DrawString("Total", _negrita, new XSolidBrush(Oscuro), x[1], l.Y + 15);
                g.DrawString(r.Productos.Sum(p => p.Unidades).ToString("N0", Cultura), _negrita, new XSolidBrush(Oscuro),
                    new XRect(x[2], l.Y + 4, ancho[2], 14), XStringFormats.TopRight);
                g.DrawString(r.Total.ToString("C2", Cultura), _negrita, new XSolidBrush(Oscuro),
                    new XRect(x[3], l.Y + 4, ancho[3], 14), XStringFormats.TopRight);
                l.Y += AltoFila;
            }
            l.Y += 18;
        }

        private void TablaPorPeriodo(Lienzo l, ReportePeriodo r)
        {
            // Posiciones: etiqueta | ventas | monto | % del total
            double[] x = { Margen, Margen + 200, Margen + 300, Margen + 455 };
            double[] ancho = { 190, 90, 150, 60 };
            var encabezados = new[] { r.PorSemana ? "SEMANA DEL" : "DÍA", "VENTAS", "MONTO", "% DEL TOTAL" };

            l.Asegurar(70);
            l.Gfx.DrawString($"Detalle por {r.UnidadBarras}", _seccion, new XSolidBrush(Oscuro), Margen, l.Y + 12);
            l.Y += 26;
            EncabezadoColumnas(l, x, ancho, encabezados, derecha: 1);

            foreach (var b in r.Barras)
            {
                if (l.Asegurar(AltoFila))
                    EncabezadoColumnas(l, x, ancho, encabezados, derecha: 1);

                var g = l.Gfx;
                var sinVentas = b.NumVentas == 0;
                var color = new XSolidBrush(sinVentas ? Gris : Oscuro);
                var pct = r.Total <= 0 ? 0 : Math.Round(b.Monto / r.Total * 100m, 1);

                g.DrawString(b.Etiqueta, _normal, color, x[0], l.Y + 15);
                g.DrawString(b.NumVentas.ToString(), _normal, color, new XRect(x[1], l.Y + 4, ancho[1], 14), XStringFormats.TopRight);
                g.DrawString(b.Monto.ToString("C2", Cultura), sinVentas ? _normal : _negrita, color,
                    new XRect(x[2], l.Y + 4, ancho[2], 14), XStringFormats.TopRight);
                g.DrawString($"{pct:0.#}%", _normal, new XSolidBrush(Gris), new XRect(x[3], l.Y + 4, ancho[3], 14), XStringFormats.TopRight);
                g.DrawLine(new XPen(Linea, 0.5), Margen, l.Y + AltoFila, AnchoPagina - Margen, l.Y + AltoFila);
                l.Y += AltoFila;
            }
        }

        /// <param name="derecha">Índice desde el cual las columnas van alineadas a la derecha (las numéricas).</param>
        private void EncabezadoColumnas(Lienzo l, double[] x, double[] ancho, string[] textos, int derecha)
        {
            var gris = new XSolidBrush(Gris);
            for (var i = 0; i < textos.Length; i++)
                l.Gfx.DrawString(textos[i], _chico, gris, new XRect(x[i], l.Y + 2, ancho[i], 14),
                    i >= derecha ? XStringFormats.TopRight : XStringFormats.TopLeft);
            l.Gfx.DrawLine(new XPen(Linea, 1), Margen, l.Y + 18, AnchoPagina - Margen, l.Y + 18);
            l.Y += 18;
        }

        /// <summary>Recorta un texto largo con "…" para que quepa en el ancho dado.</summary>
        private static string Ajustar(XGraphics g, string texto, XFont fuente, double ancho)
        {
            if (g.MeasureString(texto, fuente).Width <= ancho) return texto;
            while (texto.Length > 1 && g.MeasureString(texto + "…", fuente).Width > ancho)
                texto = texto[..^1];
            return texto + "…";
        }
    }
}

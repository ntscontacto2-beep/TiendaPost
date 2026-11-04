using System.Globalization;
using System.Windows;
using System.Windows.Media;
using PodTiendita.Services;

namespace PodTiendita.Controls
{
    /// <summary>Gráfica de barras verticales con líneas guía y etiquetas.</summary>
    public class BarChart : FrameworkElement
    {
        private const double MargenIzquierdo = 64;
        private const double MargenInferior = 26;
        private const double MargenSuperior = 12;
        private const int MaxEtiquetas = 14; // si hay más barras, se muestran solo algunas etiquetas

        private static readonly Brush ColorBarra = new SolidColorBrush(Color.FromRgb(0x1F, 0x4E, 0x79));
        private static readonly Brush ColorTexto = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
        private static readonly Pen LineaGuia = new(new SolidColorBrush(Color.FromRgb(0xE5, 0xE8, 0xEC)), 1);

        public static readonly DependencyProperty BarrasProperty = DependencyProperty.Register(
            nameof(Barras), typeof(IEnumerable<BarraVentas>), typeof(BarChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public IEnumerable<BarraVentas>? Barras
        {
            get => (IEnumerable<BarraVentas>?)GetValue(BarrasProperty);
            set => SetValue(BarrasProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            var barras = Barras?.ToList() ?? new();
            var area = new Rect(MargenIzquierdo, MargenSuperior,
                Math.Max(0, ActualWidth - MargenIzquierdo - 8),
                Math.Max(0, ActualHeight - MargenSuperior - MargenInferior));
            if (area.Width <= 0 || area.Height <= 0) return;

            var maximoDato = barras.Count == 0 ? 0 : barras.Max(b => b.Monto);
            var maximoEje = EscalaEje.Maximo(maximoDato);

            // Líneas guía y valores del eje.
            for (var i = 0; i <= EscalaEje.Divisiones; i++)
            {
                var y = area.Bottom - area.Height * i / EscalaEje.Divisiones;
                dc.DrawLine(LineaGuia, new Point(area.Left, y), new Point(area.Right, y));

                var valor = maximoEje * i / EscalaEje.Divisiones;
                var texto = Texto(valor.ToString("C0", CultureInfo.GetCultureInfo("es-MX")), 10);
                dc.DrawText(texto, new Point(area.Left - 8 - texto.Width, y - texto.Height / 2));
            }

            if (barras.Count == 0 || maximoDato <= 0)
            {
                var vacio = Texto("Sin ventas en este periodo", 12);
                dc.DrawText(vacio, new Point(area.Left + (area.Width - vacio.Width) / 2, area.Top + (area.Height - vacio.Height) / 2));
            }

            if (barras.Count == 0) return;

            var ranura = area.Width / barras.Count;
            var ancho = Math.Min(56, ranura * 0.6);
            var cadaCuantas = (int)Math.Ceiling(barras.Count / (double)MaxEtiquetas);

            for (var i = 0; i < barras.Count; i++)
            {
                var centro = area.Left + ranura * (i + 0.5);
                var alto = (double)(barras[i].Monto / maximoEje) * area.Height;
                if (alto > 0)
                    dc.DrawRectangle(ColorBarra, null, new Rect(centro - ancho / 2, area.Bottom - alto, ancho, alto));

                if (i % cadaCuantas == 0)
                {
                    var etiqueta = Texto(barras[i].Etiqueta, 10);
                    dc.DrawText(etiqueta, new Point(centro - etiqueta.Width / 2, area.Bottom + 6));
                }
            }
        }

        private FormattedText Texto(string texto, double tamano) =>
            new(texto, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"),
                tamano, ColorTexto, VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }
}

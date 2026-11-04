using System.Globalization;
using System.Windows;
using System.Windows.Media;
using PodTiendita.Services;

namespace PodTiendita.Controls
{
    /// <summary>Gráfica de dona: una rebanada por elemento y un texto al centro.</summary>
    public class PieChart : FrameworkElement
    {
        private const double ProporcionHueco = 0.62;

        public static readonly DependencyProperty RebanadasProperty = DependencyProperty.Register(
            nameof(Rebanadas), typeof(IEnumerable<RebanadaPastel>), typeof(PieChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextoCentralProperty = DependencyProperty.Register(
            nameof(TextoCentral), typeof(string), typeof(PieChart),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SubtextoCentralProperty = DependencyProperty.Register(
            nameof(SubtextoCentral), typeof(string), typeof(PieChart),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public IEnumerable<RebanadaPastel>? Rebanadas
        {
            get => (IEnumerable<RebanadaPastel>?)GetValue(RebanadasProperty);
            set => SetValue(RebanadasProperty, value);
        }

        public string TextoCentral
        {
            get => (string)GetValue(TextoCentralProperty);
            set => SetValue(TextoCentralProperty, value);
        }

        public string SubtextoCentral
        {
            get => (string)GetValue(SubtextoCentralProperty);
            set => SetValue(SubtextoCentralProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            var lado = Math.Min(ActualWidth, ActualHeight);
            if (lado <= 0) return;

            var centro = new Point(ActualWidth / 2, ActualHeight / 2);
            var radio = lado / 2;
            var rebanadas = Rebanadas?.ToList() ?? new();
            var total = rebanadas.Sum(r => r.Monto);

            if (rebanadas.Count == 0 || total <= 0)
            {
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xE9, 0xED, 0xF2)), null, centro, radio, radio);
            }
            else
            {
                var angulo = -90.0;
                var separador = new Pen(Brushes.White, 2);
                foreach (var r in rebanadas)
                {
                    var barrido = (double)(r.Monto / total) * 360.0;
                    var brush = (Brush)new BrushConverter().ConvertFromString(r.ColorHex)!;

                    if (barrido >= 359.9)
                        dc.DrawEllipse(brush, null, centro, radio, radio);
                    else
                        dc.DrawGeometry(brush, separador, Sector(centro, radio, angulo, barrido));

                    angulo += barrido;
                }
            }

            // Hueco central (el fondo de la tarjeta es blanco).
            dc.DrawEllipse(Brushes.White, null, centro, radio * ProporcionHueco, radio * ProporcionHueco);
            DibujarTexto(dc, centro);
        }

        private static Geometry Sector(Point centro, double radio, double inicioGrados, double barridoGrados)
        {
            var inicio = PuntoEnCirculo(centro, radio, inicioGrados);
            var fin = PuntoEnCirculo(centro, radio, inicioGrados + barridoGrados);

            var geometria = new StreamGeometry();
            using (var ctx = geometria.Open())
            {
                ctx.BeginFigure(centro, true, true);
                ctx.LineTo(inicio, true, false);
                ctx.ArcTo(fin, new Size(radio, radio), 0, barridoGrados > 180, SweepDirection.Clockwise, true, false);
            }
            geometria.Freeze();
            return geometria;
        }

        private static Point PuntoEnCirculo(Point centro, double radio, double grados)
        {
            var rad = grados * Math.PI / 180.0;
            return new Point(centro.X + radio * Math.Cos(rad), centro.Y + radio * Math.Sin(rad));
        }

        private void DibujarTexto(DrawingContext dc, Point centro)
        {
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var tipo = new Typeface("Segoe UI");

            var principal = new FormattedText(TextoCentral ?? "", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(tipo.FontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                16, new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37)), dpi);
            var secundario = new FormattedText(SubtextoCentral ?? "", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                tipo, 11, new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)), dpi);

            var alto = principal.Height + secundario.Height;
            dc.DrawText(principal, new Point(centro.X - principal.Width / 2, centro.Y - alto / 2));
            dc.DrawText(secundario, new Point(centro.X - secundario.Width / 2, centro.Y - alto / 2 + principal.Height));
        }
    }
}

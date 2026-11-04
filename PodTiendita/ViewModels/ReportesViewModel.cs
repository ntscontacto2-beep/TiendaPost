using System.Diagnostics;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PodTiendita.Models;
using PodTiendita.Services;

namespace PodTiendita.ViewModels
{
    /// <summary>Tarjeta de indicador: valor grande, nota y comparación con el periodo anterior.</summary>
    public record Indicador(string Titulo, string Valor, string Nota, int Tono); // Tono: 0 neutro, 1 sube, 2 baja

    /// <summary>Reportes de lo vendido en la semana, el mes o el trimestre, con descarga en PDF.</summary>
    public partial class ReportesViewModel : ObservableObject
    {
        private static readonly CultureInfo Cultura = new("es-MX");
        private const int ProductosEnPantalla = 8;

        private readonly ReporteService _reportes = new();
        private readonly PdfService _pdf = new();

        public ReportesViewModel() => Cargar();

        [ObservableProperty] private TipoPeriodo _tipo = TipoPeriodo.Semana;
        [ObservableProperty] private ReportePeriodo _reporte = new();

        // 0 = periodo actual, -1 = el anterior, etc.
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SiguienteCommand))]
        private int _desplazamiento;

        [ObservableProperty] private List<Indicador> _indicadores = new();
        [ObservableProperty] private List<RebanadaPastel> _metodos = new();
        [ObservableProperty] private List<ProductoVendido> _topProductos = new();

        public List<VentaCategoria> Categorias => Reporte.Categorias;
        public List<BarraVentas> Barras => Reporte.Barras;
        public string Etiqueta => Reporte.Etiqueta;
        public string TituloBarras => $"Ventas por {Reporte.UnidadBarras}";
        public string TotalCentral => Reporte.Total.ToString("C0", Cultura);
        public bool SinVentas => Reporte.NumVentas == 0;
        public bool HayVentas => !SinVentas;

        partial void OnTipoChanged(TipoPeriodo value)
        {
            _desplazamiento = 0; // al cambiar de tipo se vuelve al periodo actual
            OnPropertyChanged(nameof(Desplazamiento));
            SiguienteCommand.NotifyCanExecuteChanged();
            Cargar();
        }

        partial void OnDesplazamientoChanged(int value) => Cargar();

        [RelayCommand]
        private void SeleccionarTipo(string? nombre)
        {
            if (Enum.TryParse<TipoPeriodo>(nombre, out var t)) Tipo = t;
        }

        [RelayCommand]
        private void Anterior() => Desplazamiento--;

        [RelayCommand(CanExecute = nameof(PuedeAvanzar))]
        private void Siguiente() => Desplazamiento++;

        private bool PuedeAvanzar() => Desplazamiento < 0;

        private void Cargar()
        {
            Reporte = _reportes.ObtenerReporte(Tipo, Desplazamiento);
            var r = Reporte;

            Indicadores = new()
            {
                new("Ventas totales", r.Total.ToString("C2", Cultura), Comparar(r.VariacionTotal), Tono(r.VariacionTotal)),
                new("Transacciones", r.NumVentas.ToString(), Comparar(r.VariacionVentas), Tono(r.VariacionVentas)),
                new("Ticket promedio", r.TicketPromedio.ToString("C2", Cultura), Comparar(r.VariacionTicket), Tono(r.VariacionTicket)),
                new("Ventas canceladas", r.Canceladas.ToString(), "No se suman a los totales", 0),
            };

            Metodos = r.Metodos
                .Select((m, i) => new RebanadaPastel(m.Metodo.ToString(), m.Monto, m.Porcentaje, ReporteService.Paleta[i % ReporteService.Paleta.Length]))
                .ToList();
            TopProductos = r.Productos.Take(ProductosEnPantalla).ToList();

            foreach (var nombre in new[]
            {
                nameof(Categorias), nameof(Barras), nameof(Etiqueta), nameof(TituloBarras),
                nameof(TotalCentral), nameof(SinVentas), nameof(HayVentas),
            })
                OnPropertyChanged(nombre);
        }

        private static string Comparar(decimal? variacion) => variacion is null
            ? "Sin datos del periodo anterior"
            : $"{(variacion >= 0 ? "▲" : "▼")} {Math.Abs(variacion.Value):0.#}% vs. periodo anterior";

        private static int Tono(decimal? variacion) => variacion switch
        {
            null or 0 => 0,
            > 0 => 1,
            _ => 2,
        };

        [RelayCommand]
        private void DescargarPdf()
        {
            var r = Reporte;
            var nombreTipo = r.Tipo switch { TipoPeriodo.Semana => "semanal", TipoPeriodo.Mes => "mensual", _ => "trimestral" };
            var sugerido = $"Reporte_{nombreTipo}_{r.Desde:yyyy-MM-dd}_a_{r.Hasta.AddDays(-1):yyyy-MM-dd}.pdf";

            var ruta = Dialogos.PedirRutaPdf(sugerido);
            if (ruta is null) return;

            try
            {
                _pdf.GenerarReporte(r, ruta);
                Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
            }
            catch (IOException)
            {
                Dialogos.Error("No se pudo guardar el PDF. Si ya lo tienes abierto, ciérralo e inténtalo de nuevo.");
            }
        }
    }
}

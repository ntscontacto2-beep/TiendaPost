using System.Diagnostics;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PodTiendita.Models;
using PodTiendita.Services;

namespace PodTiendita.ViewModels
{
    public enum Periodo
    {
        Hoy,
        Ayer,
        EstaSemana,
        EsteMes,
    }

    /// <summary>Una fila de la tabla del historial.</summary>
    public class FilaVenta
    {
        public FilaVenta(Venta venta, bool mostrarFecha)
        {
            Venta = venta;
            Folio = $"#{venta.Id:D4}";
            Hora = venta.Fecha.ToString(mostrarFecha ? "dd/MM HH:mm" : "HH:mm");
            Cajero = venta.Usuario?.Nombre ?? "-";
            Metodo = venta.MetodoPago.ToString();
            Total = venta.Total;
            Estado = venta.Estado.ToString();
            EsCancelada = venta.Estado == EstadoVenta.Cancelada;
            // Solo el administrador cancela, y solo ventas que sigan pagadas.
            PuedeCancelar = Sesion.EsAdministrador && !EsCancelada;
        }

        public Venta Venta { get; }
        public string Folio { get; }
        public string Hora { get; }
        public string Cajero { get; }
        public string Metodo { get; }
        public decimal Total { get; }
        public string Estado { get; }
        public bool EsCancelada { get; }
        public bool PuedeCancelar { get; }
    }

    public partial class HistorialViewModel : ObservableObject
    {
        private static readonly CultureInfo Cultura = new("es-MX");
        private const string Todos = "Todos";

        private readonly ReporteService _reportes = new();
        private readonly VentaService _ventas = new();
        private readonly PdfService _pdf = new();
        private ResumenVentas _resumen = new();

        public event Action? InventarioCambiado;

        public HistorialViewModel() => Cargar();

        public static string[] MetodosPago { get; } = { Todos, nameof(Models.MetodoPago.Efectivo), nameof(Models.MetodoPago.Tarjeta) };
        public static string[] Estados { get; } = { Todos, nameof(EstadoVenta.Pagada), nameof(EstadoVenta.Cancelada) };

        // ----- Filtros -----

        [ObservableProperty] private Periodo _periodo = Periodo.Hoy;
        [ObservableProperty] private string _texto = "";
        [ObservableProperty] private string _metodoFiltro = Todos;
        [ObservableProperty] private string _estadoFiltro = Todos;

        partial void OnPeriodoChanged(Periodo value) => Cargar();
        partial void OnTextoChanged(string value) => Cargar();
        partial void OnMetodoFiltroChanged(string value) => Cargar();
        partial void OnEstadoFiltroChanged(string value) => Cargar();

        [RelayCommand]
        private void SeleccionarPeriodo(string? nombre)
        {
            if (Enum.TryParse<Periodo>(nombre, out var p)) Periodo = p;
        }

        [RelayCommand]
        private void LimpiarFiltros()
        {
            Texto = "";
            MetodoFiltro = Todos;
            EstadoFiltro = Todos;
            Periodo = Periodo.Hoy;
        }

        private (DateTime Desde, DateTime Hasta) Rango()
        {
            var hoy = DateTime.Today;
            return Periodo switch
            {
                Periodo.Ayer => (hoy.AddDays(-1), hoy),
                Periodo.EstaSemana =>
                    // La semana empieza en lunes.
                    (hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)), hoy.AddDays(7 - (((int)hoy.DayOfWeek + 6) % 7))),
                Periodo.EsteMes =>
                    (new DateTime(hoy.Year, hoy.Month, 1), new DateTime(hoy.Year, hoy.Month, 1).AddMonths(1)),
                _ => (hoy, hoy.AddDays(1)),
            };
        }

        private string NombrePeriodo => Periodo switch
        {
            Periodo.Hoy => "hoy",
            Periodo.Ayer => "ayer",
            Periodo.EstaSemana => "esta semana",
            _ => "este mes",
        };

        // ----- Resultados -----

        [ObservableProperty] private List<FilaVenta> _filas = new();
        [ObservableProperty] private List<RebanadaPastel> _rebanadas = new();
        [ObservableProperty] private int _numVentas;
        [ObservableProperty] private decimal _totalVendido;
        [ObservableProperty] private decimal _ticketPromedio;

        public string EtiquetaVentas => $"Ventas de {NombrePeriodo}";
        public string TituloGrafica => Periodo == Periodo.Hoy ? "Ventas del día" : $"Ventas de {NombrePeriodo}";
        public string ResumenTabla => $"Mostrando {Filas.Count} venta{(Filas.Count == 1 ? "" : "s")}";
        public bool HayRebanadas => Rebanadas.Count > 0;
        public bool NoHayRebanadas => !HayRebanadas;
        public bool SinVentas => Filas.Count == 0;
        public string TotalCentral => TotalVendido.ToString("C2", Cultura);

        private void Cargar()
        {
            var (desde, hasta) = Rango();
            _resumen = _reportes.Consultar(
                desde, hasta,
                Enum.TryParse<MetodoPago>(MetodoFiltro, out var m) ? m : null,
                Enum.TryParse<EstadoVenta>(EstadoFiltro, out var e) ? e : null,
                Texto);

            var variosDias = Periodo is Periodo.EstaSemana or Periodo.EsteMes;
            Filas = _resumen.Ventas.Select(v => new FilaVenta(v, variosDias)).ToList();
            Rebanadas = _resumen.Rebanadas;
            NumVentas = _resumen.NumVentas;
            TotalVendido = _resumen.Total;
            TicketPromedio = _resumen.TicketPromedio;

            foreach (var nombre in new[]
            {
                nameof(EtiquetaVentas), nameof(TituloGrafica), nameof(ResumenTabla),
                nameof(HayRebanadas), nameof(NoHayRebanadas), nameof(SinVentas), nameof(TotalCentral),
            })
                OnPropertyChanged(nombre);
        }

        // ----- Acciones -----

        [RelayCommand]
        private void VerDetalle(FilaVenta fila)
        {
            var lineas = fila.Venta.Detalles.Select(d =>
                $"• {d.Cantidad} × {d.Producto?.Nombre ?? "Producto"}  —  {d.Subtotal:C2}");
            var descuento = fila.Venta.DescuentoPorcentaje > 0 ? $"\nDescuento: {fila.Venta.DescuentoPorcentaje:0.##}%" : "";

            Dialogos.Informar(
                $"Venta {fila.Folio} · {fila.Venta.Fecha:dd/MM/yyyy HH:mm}\n" +
                $"Cajero: {fila.Cajero} · {fila.Metodo} · {fila.Estado}\n\n" +
                string.Join("\n", lineas) + descuento +
                $"\n\nTotal: {fila.Total:C2} MXN");
        }

        [RelayCommand]
        private void CancelarVenta(FilaVenta fila)
        {
            if (Sesion.Actual is null) return;
            if (!Dialogos.Confirmar($"¿Cancelar la venta {fila.Folio} por {fila.Total:C2}?\nEl stock de los productos regresará al inventario.")) return;

            try
            {
                _ventas.Cancelar(fila.Venta.Id, Sesion.Actual.Id);
                InventarioCambiado?.Invoke();
            }
            catch (NegocioException ex)
            {
                Dialogos.Error(ex.Message);
            }
            Cargar();
        }

        [RelayCommand]
        private void DescargarPdf()
        {
            var sufijo = Periodo == Periodo.Hoy
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : $"{_resumen.Desde:yyyy-MM-dd}_a_{_resumen.Hasta.AddDays(-1):yyyy-MM-dd}";

            var ruta = Dialogos.PedirRutaPdf($"Ventas_{sufijo}.pdf");
            if (ruta is null) return;

            try
            {
                var rango = Periodo == Periodo.Hoy
                    ? DateTime.Today.ToString("dddd d 'de' MMMM yyyy", Cultura)
                    : $"{_resumen.Desde:dd/MM/yyyy} al {_resumen.Hasta.AddDays(-1):dd/MM/yyyy}";

                _pdf.Generar(_resumen, $"Reporte de ventas · {rango}", ruta);
                Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
            }
            catch (IOException)
            {
                Dialogos.Error("No se pudo guardar el PDF. Si ya lo tienes abierto, ciérralo e inténtalo de nuevo.");
            }
        }
    }
}

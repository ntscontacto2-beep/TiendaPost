using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PodTiendita.Models;
using PodTiendita.Services;

namespace PodTiendita.ViewModels
{
    /// <summary>Ventana base: sesión, reloj, alertas de stock y navegación entre pantallas.</summary>
    public partial class MainViewModel : ObservableObject
    {
        private static readonly CultureInfo Cultura = new("es-MX");

        private readonly ProductoService _productos = new();

        public MainViewModel()
        {
            var reloj = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            reloj.Tick += (_, _) => ActualizarReloj();
            reloj.Start();
            ActualizarReloj();

            RefrescarAlertas();
            IrAVentas();
        }

        // ----- Sesión -----

        public string NombreUsuario => Sesion.Actual?.Nombre ?? "";
        public string RolUsuario => Sesion.Actual?.Rol.ToString() ?? "";
        public string Iniciales => NombreUsuario.Length > 0 ? NombreUsuario[..1].ToUpper() : "?";
        public bool EsAdministrador => Sesion.EsAdministrador;

        [ObservableProperty]
        private string _reloj = "";

        private void ActualizarReloj() =>
            Reloj = DateTime.Now.ToString("ddd, d MMM · HH:mm", Cultura);

        // ----- Alertas de stock bajo (campana) -----

        public ObservableCollection<Producto> StockBajo { get; } = new();
        public bool HayStockBajo => StockBajo.Count > 0;

        private void RefrescarAlertas()
        {
            StockBajo.Clear();
            foreach (var p in _productos.ObtenerActivos().Where(p => p.StockBajo).OrderBy(p => p.Stock))
                StockBajo.Add(p);
            OnPropertyChanged(nameof(HayStockBajo));
        }

        // ----- Navegación -----

        /// <summary>ViewModel de la pantalla visible; la vista se elige con un DataTemplate.</summary>
        [ObservableProperty]
        private object? _vistaActual;

        [RelayCommand]
        private void IrAVentas()
        {
            var vm = new VentasViewModel();
            vm.InventarioCambiado += RefrescarAlertas;
            VistaActual = vm;
        }

        [RelayCommand]
        private void IrAHistorial()
        {
            var vm = new HistorialViewModel();
            vm.InventarioCambiado += RefrescarAlertas;
            VistaActual = vm;
        }

        [RelayCommand]
        private void IrAProductos()
        {
            var lista = new ProductosViewModel();
            lista.InventarioCambiado += RefrescarAlertas;
            lista.NuevoSolicitado += () => AbrirFormulario(null);
            lista.EditarSolicitado += producto => AbrirFormulario(producto);
            VistaActual = lista;
        }

        [RelayCommand]
        private void IrAReportes() => VistaActual = new ReportesViewModel();

        private void AbrirFormulario(Producto? producto)
        {
            var form = new ProductoFormViewModel(producto);
            form.Cerrado += guardado =>
            {
                if (guardado) RefrescarAlertas();
                IrAProductos();
            };
            VistaActual = form;
        }
    }
}

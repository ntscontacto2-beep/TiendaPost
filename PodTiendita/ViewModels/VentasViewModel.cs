using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PodTiendita.Models;
using PodTiendita.Services;

namespace PodTiendita.ViewModels
{
    /// <summary>Pantalla de venta: catálogo, carrito y cobro.</summary>
    public partial class VentasViewModel : ObservableObject
    {
        private readonly ProductoService _productos = new();
        private readonly VentaService _ventas = new();
        private List<Producto> _catalogo = new();

        /// <summary>Se dispara cuando cambia el stock, para que la campana de alertas se refresque.</summary>
        public event Action? InventarioCambiado;

        public VentasViewModel()
        {
            Carrito.CollectionChanged += Carrito_CollectionChanged;
            RecargarCatalogo();
        }

        public bool EsAdministrador => Sesion.EsAdministrador;

        // ----- Catálogo -----

        [ObservableProperty]
        private List<Producto> _productosVisibles = new();

        [ObservableProperty]
        private string _busqueda = "";

        [ObservableProperty]
        private CategoriaProducto? _categoria;

        partial void OnBusquedaChanged(string value) => Filtrar();
        partial void OnCategoriaChanged(CategoriaProducto? value) => Filtrar();

        [RelayCommand]
        private void SeleccionarCategoria(string? nombre) =>
            Categoria = Enum.TryParse<CategoriaProducto>(nombre, out var c) ? c : null;

        private void RecargarCatalogo()
        {
            _catalogo = _productos.ObtenerActivos();
            Filtrar();
        }

        private void Filtrar()
        {
            var texto = Busqueda.Trim();
            ProductosVisibles = _catalogo
                .Where(p => Categoria is null || p.Categoria == Categoria)
                .Where(p => texto.Length == 0 || p.Nombre.Contains(texto, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        // ----- Carrito -----

        public ObservableCollection<LineaCarrito> Carrito { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MontoDescuento), nameof(Total))]
        private decimal _descuentoPorcentaje;

        [ObservableProperty]
        private bool _pagoEnEfectivo = true;

        public int Articulos => Carrito.Sum(l => l.Cantidad);
        public decimal Subtotal => Carrito.Sum(l => l.Subtotal);
        public decimal DescuentoAplicado => Math.Clamp(DescuentoPorcentaje, 0, 100);
        public decimal MontoDescuento => Math.Round(Subtotal * DescuentoAplicado / 100m, 2);
        public decimal Total => Subtotal - MontoDescuento;
        public bool CarritoVacio => Carrito.Count == 0;

        private void Carrito_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
                foreach (LineaCarrito l in e.NewItems) l.PropertyChanged += Linea_PropertyChanged;
            if (e.OldItems is not null)
                foreach (LineaCarrito l in e.OldItems) l.PropertyChanged -= Linea_PropertyChanged;
            NotificarTotales();
        }

        private void Linea_PropertyChanged(object? sender, PropertyChangedEventArgs e) => NotificarTotales();

        private void NotificarTotales()
        {
            OnPropertyChanged(nameof(Articulos));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(MontoDescuento));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(CarritoVacio));
            CobrarCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(PuedeAgregar))]
        private void Agregar(Producto producto)
        {
            var linea = Carrito.FirstOrDefault(l => l.Producto.Id == producto.Id);
            if (linea is null)
                Carrito.Add(new LineaCarrito(producto));
            else if (linea.Cantidad < producto.Stock)
                linea.Cantidad++;
        }

        private static bool PuedeAgregar(Producto? producto) => producto is { SinStock: false };

        [RelayCommand]
        private void Mas(LineaCarrito linea)
        {
            if (linea.Cantidad < linea.Producto.Stock)
                linea.Cantidad++;
        }

        [RelayCommand]
        private void Menos(LineaCarrito linea)
        {
            if (linea.Cantidad > 1)
                linea.Cantidad--;
            else
                Carrito.Remove(linea);
        }

        [RelayCommand]
        private void Quitar(LineaCarrito linea) => Carrito.Remove(linea);

        [RelayCommand]
        private void Cancelar()
        {
            if (Carrito.Count > 0 && !Dialogos.Confirmar("¿Cancelar la venta actual?")) return;
            LimpiarVenta();
        }

        private void LimpiarVenta()
        {
            Carrito.Clear();
            DescuentoPorcentaje = 0;
        }

        [RelayCommand(CanExecute = nameof(PuedeCobrar))]
        private void Cobrar()
        {
            var usuario = Sesion.Actual;
            if (usuario is null) return;

            var lineas = Carrito.Select(l => new LineaVenta(l.Producto.Id, l.Cantidad)).ToList();
            var vendidos = lineas.Select(l => l.ProductoId).ToHashSet();
            var metodo = PagoEnEfectivo ? MetodoPago.Efectivo : MetodoPago.Tarjeta;
            // Solo el administrador puede dar descuentos.
            var descuento = usuario.Rol == Rol.Administrador ? DescuentoAplicado : 0;

            try
            {
                var venta = _ventas.Registrar(usuario.Id, lineas, metodo, descuento);
                LimpiarVenta();
                RecargarCatalogo();
                InventarioCambiado?.Invoke();

                Dialogos.Informar($"Venta #{venta.Id:D4} cobrada con {metodo}.\nTotal: {venta.Total:C2} MXN");
                AvisarStockBajo(vendidos);
            }
            catch (NegocioException ex)
            {
                // El stock pudo haber cambiado: se refresca para que la pantalla refleje la realidad.
                RecargarCatalogo();
                InventarioCambiado?.Invoke();
                Dialogos.Error(ex.Message);
            }
        }

        private bool PuedeCobrar() => Carrito.Count > 0;

        private void AvisarStockBajo(HashSet<int> vendidos)
        {
            var alertas = _catalogo.Where(p => vendidos.Contains(p.Id) && p.StockBajo).ToList();
            if (alertas.Count == 0) return;

            var detalle = string.Join("\n", alertas.Select(p =>
                p.SinStock ? $"• {p.Nombre}: AGOTADO" : $"• {p.Nombre}: quedan {p.Stock}"));
            Dialogos.Advertir($"Stock bajo, hay que resurtir:\n{detalle}");
        }
    }
}

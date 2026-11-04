using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PodTiendita.Models;
using PodTiendita.Services;

namespace PodTiendita.ViewModels
{
    public enum FiltroProducto
    {
        Todos,
        Disponibles,
        BajoStock,
        Agotados,
    }

    /// <summary>Una fila de la tabla de productos.</summary>
    public class FilaProducto
    {
        public FilaProducto(Producto producto)
        {
            Producto = producto;
            Existencia = $"{producto.Stock} pza{(producto.Stock == 1 ? "" : "s")}.";
            Agotado = producto.SinStock;
            BajoStock = producto.StockBajo && !producto.SinStock;
            Estado = Agotado ? "Agotado" : BajoStock ? "Stock bajo" : "Disponible";
        }

        public Producto Producto { get; }
        public string Existencia { get; }
        public string Estado { get; }
        public bool Agotado { get; }
        public bool BajoStock { get; }
    }

    /// <summary>Catálogo de productos: lista, búsqueda y filtros. Solo el administrador puede modificarlo.</summary>
    public partial class ProductosViewModel : ObservableObject
    {
        private readonly ProductoService _productos = new();
        private List<Producto> _catalogo = new();

        public event Action? NuevoSolicitado;
        public event Action<Producto>? EditarSolicitado;
        public event Action? InventarioCambiado;

        public ProductosViewModel() => Recargar();

        public bool EsAdministrador => Sesion.EsAdministrador;

        [ObservableProperty] private string _busqueda = "";
        [ObservableProperty] private FiltroProducto _filtro = FiltroProducto.Todos;
        [ObservableProperty] private List<FilaProducto> _filas = new();

        public bool SinResultados => Filas.Count == 0;
        public string ResumenTabla => $"Mostrando {Filas.Count} de {_catalogo.Count} producto{(_catalogo.Count == 1 ? "" : "s")}";

        partial void OnBusquedaChanged(string value) => Filtrar();
        partial void OnFiltroChanged(FiltroProducto value) => Filtrar();

        [RelayCommand]
        private void SeleccionarFiltro(string? nombre)
        {
            if (Enum.TryParse<FiltroProducto>(nombre, out var f)) Filtro = f;
        }

        private void Recargar()
        {
            _catalogo = _productos.ObtenerActivos();
            Filtrar();
        }

        private void Filtrar()
        {
            var texto = Busqueda.Trim();

            Filas = _catalogo
                .Where(p => Filtro switch
                {
                    FiltroProducto.Disponibles => !p.StockBajo,
                    FiltroProducto.BajoStock => p.StockBajo && !p.SinStock,
                    FiltroProducto.Agotados => p.SinStock,
                    _ => true,
                })
                .Where(p => texto.Length == 0 ||
                            p.Nombre.Contains(texto, StringComparison.CurrentCultureIgnoreCase) ||
                            p.Categoria.ToString().Contains(texto, StringComparison.CurrentCultureIgnoreCase))
                .Select(p => new FilaProducto(p))
                .ToList();

            OnPropertyChanged(nameof(SinResultados));
            OnPropertyChanged(nameof(ResumenTabla));
        }

        [RelayCommand]
        private void Nuevo() => NuevoSolicitado?.Invoke();

        [RelayCommand]
        private void Editar(FilaProducto fila) => EditarSolicitado?.Invoke(fila.Producto);

        [RelayCommand]
        private void Eliminar(FilaProducto fila)
        {
            if (Sesion.Actual is null) return;
            if (!Dialogos.Confirmar($"¿Eliminar \"{fila.Producto.Nombre}\" del catálogo?\n\nYa no aparecerá para vender. Las ventas anteriores no se modifican.")) return;

            try
            {
                _productos.Eliminar(fila.Producto.Id, Sesion.Actual.Id);
                InventarioCambiado?.Invoke();
            }
            catch (NegocioException ex)
            {
                Dialogos.Error(ex.Message);
            }
            Recargar();
        }
    }
}

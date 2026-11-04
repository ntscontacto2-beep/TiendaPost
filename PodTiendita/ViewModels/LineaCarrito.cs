using CommunityToolkit.Mvvm.ComponentModel;
using PodTiendita.Models;

namespace PodTiendita.ViewModels
{
    public partial class LineaCarrito : ObservableObject
    {
        public LineaCarrito(Producto producto) => Producto = producto;

        public Producto Producto { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Subtotal))]
        private int _cantidad = 1;

        public decimal Subtotal => Producto.Precio * Cantidad;
    }
}

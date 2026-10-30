using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PodTiendita
{
    public class Product
    {
        public string Name { get; init; } = "";
        public string Category { get; init; } = "";
        public string Description { get; init; } = "";
        public decimal Price { get; init; }
        public int Stock { get; init; }

        public string Initial => Name.Length > 0 ? Name[..1].ToUpper() : "?";
    }

    public class CartItem : INotifyPropertyChanged
    {
        private int _quantity = 1;

        public CartItem(Product product) => Product = product;

        public Product Product { get; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value) return;
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public decimal Subtotal => Product.Price * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

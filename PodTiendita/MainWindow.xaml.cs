using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;

namespace PodTiendita
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private const string TodasLasCategorias = "Todos";

        private readonly List<Product> _catalogo = CrearCatalogo();
        private readonly ObservableCollection<CartItem> _carrito = new();
        private string _categoria = TodasLasCategorias;
        private int _folio = 1001;

        public MainWindow()
        {
            InitializeComponent();

            lstCarrito.ItemsSource = _carrito;
            CrearCategorias();
            AplicarFiltro();
            ActualizarTotales();
        }

        private static List<Product> CrearCatalogo() => new()
        {
            new() { Name = "Arroz 1 kg", Category = "Abarrotes", Description = "Arroz blanco grano largo.", Price = 28.50m, Stock = 40 },
            new() { Name = "Frijol negro 1 kg", Category = "Abarrotes", Description = "Frijol negro seleccionado.", Price = 34.00m, Stock = 35 },
            new() { Name = "Aceite 900 ml", Category = "Abarrotes", Description = "Aceite vegetal comestible.", Price = 42.90m, Stock = 22 },
            new() { Name = "Azúcar 1 kg", Category = "Abarrotes", Description = "Azúcar estándar.", Price = 26.00m, Stock = 50 },
            new() { Name = "Refresco cola 600 ml", Category = "Bebidas", Description = "Refresco sabor cola.", Price = 18.00m, Stock = 60 },
            new() { Name = "Agua 1 L", Category = "Bebidas", Description = "Agua purificada.", Price = 12.00m, Stock = 80 },
            new() { Name = "Jugo de naranja 1 L", Category = "Bebidas", Description = "Jugo néctar de naranja.", Price = 32.00m, Stock = 18 },
            new() { Name = "Papas fritas", Category = "Botanas", Description = "Papas sabor sal, 45 g.", Price = 19.00m, Stock = 45 },
            new() { Name = "Cacahuates", Category = "Botanas", Description = "Cacahuates salados, 100 g.", Price = 16.50m, Stock = 30 },
            new() { Name = "Galletas surtidas", Category = "Botanas", Description = "Paquete de galletas dulces.", Price = 24.00m, Stock = 28 },
            new() { Name = "Leche entera 1 L", Category = "Lácteos", Description = "Leche entera pasteurizada.", Price = 27.50m, Stock = 36 },
            new() { Name = "Yogurt natural", Category = "Lácteos", Description = "Yogurt natural 900 g.", Price = 38.00m, Stock = 14 },
            new() { Name = "Queso panela 400 g", Category = "Lácteos", Description = "Queso panela fresco.", Price = 65.00m, Stock = 10 },
            new() { Name = "Detergente 1 kg", Category = "Limpieza", Description = "Detergente en polvo.", Price = 48.00m, Stock = 20 },
            new() { Name = "Cloro 1 L", Category = "Limpieza", Description = "Blanqueador líquido.", Price = 21.00m, Stock = 25 },
            new() { Name = "Jabón de trastes", Category = "Limpieza", Description = "Lavatrastes líquido 750 ml.", Price = 29.00m, Stock = 17 },
        };

        // ----- Catálogo -----

        private void CrearCategorias()
        {
            var nombres = new[] { TodasLasCategorias }
                .Concat(_catalogo.Select(p => p.Category).Distinct());

            foreach (var nombre in nombres)
            {
                var chip = new RadioButton
                {
                    Content = nombre,
                    Tag = nombre,
                    GroupName = "categorias",
                    Style = (Style)FindResource("PillButton"),
                    IsChecked = nombre == _categoria,
                };
                chip.Checked += Categoria_Checked;
                pnlCategorias.Children.Add(chip);
            }
        }

        private void Categoria_Checked(object sender, RoutedEventArgs e)
        {
            _categoria = (string)((RadioButton)sender).Tag;
            AplicarFiltro();
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltro();

        private void AplicarFiltro()
        {
            var texto = txtBuscar.Text.Trim();

            lstProductos.ItemsSource = _catalogo
                .Where(p => _categoria == TodasLasCategorias || p.Category == _categoria)
                .Where(p => texto.Length == 0 || p.Name.Contains(texto, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            lblTitulo.Text = _categoria == TodasLasCategorias ? "Todos los productos" : _categoria;
        }

        // ----- Carrito -----

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            var producto = (Product)((FrameworkElement)sender).DataContext;
            var linea = _carrito.FirstOrDefault(c => c.Product == producto);

            if (linea is null)
                _carrito.Add(new CartItem(producto));
            else if (linea.Quantity < producto.Stock)
                linea.Quantity++;

            ActualizarTotales();
        }

        private void BtnMas_Click(object sender, RoutedEventArgs e)
        {
            var linea = (CartItem)((FrameworkElement)sender).DataContext;
            if (linea.Quantity < linea.Product.Stock)
                linea.Quantity++;
            ActualizarTotales();
        }

        private void BtnMenos_Click(object sender, RoutedEventArgs e)
        {
            var linea = (CartItem)((FrameworkElement)sender).DataContext;
            if (linea.Quantity > 1)
                linea.Quantity--;
            else
                _carrito.Remove(linea);
            ActualizarTotales();
        }

        private void BtnVaciar_Click(object sender, RoutedEventArgs e)
        {
            _carrito.Clear();
            ActualizarTotales();
        }

        private void ActualizarTotales()
        {
            lblArticulos.Text = _carrito.Sum(c => c.Quantity).ToString();
            lblTotal.Text = _carrito.Sum(c => c.Subtotal).ToString("C2");
            lblVacio.Visibility = _carrito.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            btnCobrar.IsEnabled = _carrito.Count > 0;
        }

        private void BtnCobrar_Click(object sender, RoutedEventArgs e)
        {
            var metodo = rbTarjeta.IsChecked == true ? "Tarjeta" : "Efectivo";

            // TODO: registrar la venta y descontar existencias.
            MessageBox.Show(
                $"Venta #{_folio} cobrada con {metodo}.\nTotal: {lblTotal.Text}",
                "PodTiendita", MessageBoxButton.OK, MessageBoxImage.Information);

            _folio++;
            lblFolio.Text = $"Folio #{_folio}";
            _carrito.Clear();
            ActualizarTotales();
        }

        // ----- Sesión -----

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            new Window1().Show();
            Close();
        }
    }
}

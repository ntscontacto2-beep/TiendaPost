using System.Windows;
using System.Windows.Controls;
using PodTiendita.ViewModels;

namespace PodTiendita.Views
{
    public partial class ProductoFormView : UserControl
    {
        private Window? _ventana;

        public ProductoFormView()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                _ventana = Window.GetWindow(this);
                if (_ventana is not null) _ventana.Activated += Ventana_Activated;
            };
            Unloaded += (_, _) =>
            {
                if (_ventana is not null) _ventana.Activated -= Ventana_Activated;
                _ventana = null;
            };
        }

        // Al regresar del navegador se busca sola la foto que copiaron, para ahorrarles un paso.
        private void Ventana_Activated(object? sender, EventArgs e)
        {
            if (DataContext is ProductoFormViewModel vm)
                vm.AlVolverALaApp();
        }
    }
}

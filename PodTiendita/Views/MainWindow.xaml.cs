using System.Windows;
using PodTiendita.Services;
using PodTiendita.ViewModels;
using Wpf.Ui.Controls;

namespace PodTiendita.Views
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            Sesion.Cerrar();
            new LoginWindow().Show();
            Close();
        }
    }
}

using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace PodTiendita
{
    /// <summary>
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class Window1 : FluentWindow
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            e.Handled = true;
            BtnIngresar_Click(sender, e);
        }

        private void BtnIngresar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrEmpty(txtPassword.Password))
            {
                lblError.Text = "Captura usuario y contraseña.";
                lblError.Visibility = Visibility.Visible;
                return;
            }

            // TODO: validar credenciales reales antes de continuar.
            lblError.Visibility = Visibility.Collapsed;
            new MainWindow().Show();
            Close();
        }
    }
}

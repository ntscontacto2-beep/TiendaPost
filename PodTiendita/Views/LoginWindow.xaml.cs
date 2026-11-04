using System.Windows;
using System.Windows.Input;
using PodTiendita.Services;
using Wpf.Ui.Controls;

namespace PodTiendita.Views
{
    public partial class LoginWindow : FluentWindow
    {
        private readonly AuthService _auth = new();

        public LoginWindow()
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
                MostrarError("Captura usuario y contraseña.");
                return;
            }

            var usuario = _auth.Autenticar(txtUsuario.Text, txtPassword.Password);
            if (usuario is null)
            {
                MostrarError("Usuario o contraseña incorrectos.");
                txtPassword.Password = "";
                return;
            }

            Sesion.Iniciar(usuario);
            new MainWindow().Show();
            Close();
        }

        private void MostrarError(string mensaje)
        {
            lblError.Text = mensaje;
            lblError.Visibility = Visibility.Visible;
        }
    }
}

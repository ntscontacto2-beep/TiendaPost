using System.Windows;

namespace PodTiendita.Services
{
    public static class Dialogos
    {
        private const string Titulo = "PodTiendita";

        public static void Informar(string mensaje) =>
            MessageBox.Show(mensaje, Titulo, MessageBoxButton.OK, MessageBoxImage.Information);

        public static void Advertir(string mensaje) =>
            MessageBox.Show(mensaje, Titulo, MessageBoxButton.OK, MessageBoxImage.Warning);

        public static void Error(string mensaje) =>
            MessageBox.Show(mensaje, Titulo, MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>Pide una imagen de la computadora. Devuelve null si el usuario cancela.</summary>
        public static string? PedirRutaImagen()
        {
            var dialogo = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Elige la foto del producto",
                Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            };
            return dialogo.ShowDialog() == true ? dialogo.FileName : null;
        }

        /// <summary>Pide dónde guardar un PDF. Devuelve null si el usuario cancela.</summary>
        public static string? PedirRutaPdf(string nombreSugerido)
        {
            var dialogo = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Guardar reporte",
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = nombreSugerido,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };
            return dialogo.ShowDialog() == true ? dialogo.FileName : null;
        }

        public static bool Confirmar(string mensaje) =>
            MessageBox.Show(mensaje, Titulo, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}

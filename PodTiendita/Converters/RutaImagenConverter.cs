using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PodTiendita.Data;

namespace PodTiendita.Converters
{
    /// <summary>Convierte el nombre de archivo guardado en el producto en una imagen. Sin archivo devuelve null.</summary>
    public class RutaImagenConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string nombre || nombre.Length == 0) return null;

            var ruta = Path.Combine(DbPaths.CarpetaImagenes, Path.GetFileName(nombre));
            if (!File.Exists(ruta)) return null;

            var imagen = new BitmapImage();
            imagen.BeginInit();
            imagen.CacheOption = BitmapCacheOption.OnLoad; // no deja el archivo bloqueado
            imagen.DecodePixelWidth = 300;
            imagen.UriSource = new Uri(ruta);
            imagen.EndInit();
            imagen.Freeze();
            return imagen;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}

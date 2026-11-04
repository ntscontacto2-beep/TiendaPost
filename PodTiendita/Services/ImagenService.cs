using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PodTiendita.Data;

namespace PodTiendita.Services
{
    /// <summary>Guarda las fotos de los productos en la carpeta de imágenes de la app.</summary>
    public class ImagenService
    {
        private const long MaxBytes = 5 * 1024 * 1024;
        private const int AnchoMaximo = 800; // suficiente para las tarjetas; evita fotos enormes en disco
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        /// <summary>Copia un archivo que el usuario eligió. Devuelve el nombre guardado.</summary>
        public string GuardarDesdeArchivo(string ruta)
        {
            var extension = Path.GetExtension(ruta).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                throw new NegocioException("Elige una imagen JPG, PNG, BMP o GIF.");
            if (new FileInfo(ruta).Length > MaxBytes)
                throw new NegocioException("La imagen pesa más de 5 MB. Elige una más pequeña.");

            return Escribir(File.ReadAllBytes(ruta), extension);
        }

        /// <summary>
        /// Toma la imagen que el usuario copió en el navegador ("Copiar imagen") y la deja en un archivo
        /// temporal para mostrarla como vista previa. Devuelve null si no hay ninguna imagen copiada.
        /// Se guarda de forma definitiva solo cuando se guarda el producto.
        /// </summary>
        public string? TomarDelPortapapeles()
        {
            BitmapSource? imagen;
            try
            {
                if (!Clipboard.ContainsImage()) return null;
                imagen = Clipboard.GetImage();
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                return null; // el portapapeles estaba ocupado por otro programa; se puede reintentar
            }
            if (imagen is null) return null;

            if (imagen.PixelWidth > AnchoMaximo)
            {
                var escala = (double)AnchoMaximo / imagen.PixelWidth;
                imagen = new TransformedBitmap(imagen, new ScaleTransform(escala, escala));
            }

            var codificador = new PngBitmapEncoder();
            codificador.Frames.Add(BitmapFrame.Create(imagen));
            var ruta = Path.Combine(Path.GetTempPath(), $"pod_{Guid.NewGuid():N}.png");
            using (var archivo = File.Create(ruta))
                codificador.Save(archivo);

            return ruta;
        }

        /// <summary>Borra una imagen de la carpeta de la app (ignora nombres vacíos o inexistentes).</summary>
        public void Eliminar(string? nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return;
            var ruta = Path.Combine(DbPaths.CarpetaImagenes, Path.GetFileName(nombre));
            if (File.Exists(ruta)) File.Delete(ruta);
        }

        private static string Escribir(byte[] bytes, string extension)
        {
            if (bytes.Length > MaxBytes)
                throw new NegocioException("La imagen pesa más de 5 MB. Elige una más pequeña.");

            // Comprueba que de verdad sea una imagen que WPF pueda mostrar.
            try
            {
                using var ms = new MemoryStream(bytes);
                BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            catch (Exception ex) when (ex is NotSupportedException or FileFormatException or InvalidOperationException)
            {
                throw new NegocioException("Ese archivo no es una imagen válida.");
            }

            var nombre = $"{Guid.NewGuid():N}{extension}";
            File.WriteAllBytes(Path.Combine(DbPaths.CarpetaImagenes, nombre), bytes);
            return nombre;
        }
    }
}

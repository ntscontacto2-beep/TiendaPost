using System.IO;

namespace PodTiendita.Data
{
    public static class DbPaths
    {
        /// <summary>
        /// %LocalAppData%\PodTiendita (carpeta siempre con permiso de escritura).
        /// La variable PODTIENDITA_DATOS permite usar otra carpeta (útil para pruebas).
        /// </summary>
        public static string CarpetaDatos
        {
            get
            {
                var carpeta = Environment.GetEnvironmentVariable("PODTIENDITA_DATOS") ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PodTiendita");
                Directory.CreateDirectory(carpeta);
                return carpeta;
            }
        }

        public static string Archivo => Path.Combine(CarpetaDatos, "tiendita.db");

        public static string CadenaConexion => $"Data Source={Archivo}";

        /// <summary>Aquí se guardan las imágenes de los productos.</summary>
        public static string CarpetaImagenes
        {
            get
            {
                var carpeta = Path.Combine(CarpetaDatos, "imagenes");
                Directory.CreateDirectory(carpeta);
                return carpeta;
            }
        }
    }
}

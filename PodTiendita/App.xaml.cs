using System.Windows;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using PodTiendita.Data;
using Wpf.Ui.Appearance;

namespace PodTiendita
{
    public partial class App : Application
    {
        public static DbContextOptions<AppDbContext> DbOptions { get; } =
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(DbPaths.CadenaConexion).Options;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Color de acento (foco, selección) acorde a la paleta de la app.
            ApplicationAccentColorManager.Apply(Color.FromRgb(0x1F, 0x4E, 0x79), ApplicationTheme.Light);

            try
            {
                using var db = new AppDbContext(DbOptions);
                DbInitializer.Inicializar(db);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo iniciar la base de datos:\n{ex.Message}", "PodTiendita",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}

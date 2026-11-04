using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PodTiendita.Data
{
    /// <summary>Solo lo usa `dotnet ef` para crear migraciones.</summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(DbPaths.CadenaConexion)
                .Options;
            return new AppDbContext(options);
        }
    }
}

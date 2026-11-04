using Microsoft.EntityFrameworkCore;
using PodTiendita.Models;
using PodTiendita.Services;

namespace PodTiendita.Data
{
    public static class DbInitializer
    {
        /// <summary>Aplica migraciones pendientes y siembra datos iniciales la primera vez.</summary>
        public static void Inicializar(AppDbContext db)
        {
            db.Database.Migrate();

            if (!db.Usuarios.Any())
            {
                // Contraseña inicial: cambiarla desde la app en el primer uso.
                db.Usuarios.Add(new Usuario
                {
                    NombreUsuario = "admin",
                    Nombre = "Administrador",
                    PasswordHash = PasswordHasher.Hash("admin123"),
                    Rol = Rol.Administrador,
                });
            }

            if (!db.Productos.Any())
                db.Productos.AddRange(ProductosDeEjemplo());

            db.SaveChanges();
        }

        private static IEnumerable<Producto> ProductosDeEjemplo()
        {
            Producto P(string nombre, string desc, CategoriaProducto cat, decimal precio, int stock, int min = 5) =>
                new() { Nombre = nombre, Descripcion = desc, Categoria = cat, Precio = precio, Stock = stock, StockMinimo = min };

            return new[]
            {
                P("Arroz 1 kg", "Arroz blanco grano largo.", CategoriaProducto.Abarrotes, 28.50m, 40),
                P("Frijol negro 1 kg", "Frijol negro seleccionado.", CategoriaProducto.Abarrotes, 34.00m, 35),
                P("Aceite 900 ml", "Aceite vegetal comestible.", CategoriaProducto.Abarrotes, 42.90m, 22),
                P("Azúcar 1 kg", "Azúcar estándar.", CategoriaProducto.Abarrotes, 26.00m, 50),
                P("Refresco cola 600 ml", "Refresco sabor cola.", CategoriaProducto.Abarrotes, 18.00m, 10, 3),
                P("Agua 1 L", "Agua purificada.", CategoriaProducto.Abarrotes, 12.00m, 80),
                P("Papas fritas", "Papas sabor sal, 45 g.", CategoriaProducto.Abarrotes, 19.00m, 45),
                P("Leche entera 1 L", "Leche entera pasteurizada.", CategoriaProducto.Abarrotes, 27.50m, 36),
                P("Detergente 1 kg", "Detergente en polvo.", CategoriaProducto.Abarrotes, 48.00m, 20),
                P("Cerveza clara 355 ml", "Cerveza en lata.", CategoriaProducto.Licores, 24.00m, 48, 12),
                P("Tequila blanco 750 ml", "Tequila 100% agave.", CategoriaProducto.Licores, 320.00m, 8, 2),
                P("Ron añejo 750 ml", "Ron añejo.", CategoriaProducto.Licores, 280.00m, 6, 2),
            };
        }
    }
}

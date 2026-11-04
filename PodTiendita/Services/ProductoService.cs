using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PodTiendita.Data;
using PodTiendita.Models;

namespace PodTiendita.Services
{
    /// <summary>Datos editables de un producto (lo que captura el formulario).</summary>
    public record ProductoDatos(
        string Nombre,
        string Descripcion,
        CategoriaProducto Categoria,
        decimal Precio,
        int Stock,
        int StockMinimo,
        string? ImagenRuta);

    public class ProductoService
    {
        private static readonly CultureInfo Cultura = new("es-MX");
        private const int MaxDetalle = 500; // largo de Movimiento.Detalle

        public List<Producto> ObtenerActivos()
        {
            using var db = new AppDbContext(App.DbOptions);
            return db.Productos.AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToList();
        }

        public Producto Crear(ProductoDatos datos, int usuarioId)
        {
            Validar(datos);

            using var db = new AppDbContext(App.DbOptions);
            Permisos.ExigirAdministrador(db, usuarioId, "agregar productos");
            ValidarNombreUnico(db, datos.Nombre, null);

            var producto = new Producto
            {
                Nombre = datos.Nombre.Trim(),
                Descripcion = datos.Descripcion.Trim(),
                Categoria = datos.Categoria,
                Precio = datos.Precio,
                Stock = datos.Stock,
                StockMinimo = datos.StockMinimo,
                ImagenRuta = datos.ImagenRuta,
            };
            db.Productos.Add(producto);
            db.SaveChanges(); // para tener el Id antes de escribir la bitácora

            db.Movimientos.Add(Movimiento(TipoMovimiento.AltaProducto, usuarioId, producto, null, producto.Stock,
                $"Alta: {producto.Categoria}, precio {Dinero(producto.Precio)}, existencia inicial {producto.Stock}"));
            db.SaveChanges();
            return producto;
        }

        /// <param name="stockOriginal">Existencia que tenía el producto cuando se abrió el formulario.</param>
        public void Actualizar(int id, ProductoDatos datos, int stockOriginal, int usuarioId)
        {
            Validar(datos);

            using var db = new AppDbContext(App.DbOptions);
            using var tx = db.Database.BeginTransaction();
            Permisos.ExigirAdministrador(db, usuarioId, "editar productos");

            var producto = db.Productos.Find(id);
            if (producto is not { Activo: true })
                throw new NegocioException("El producto ya no existe.");
            ValidarNombreUnico(db, datos.Nombre, id);

            var cambios = new List<string>();
            void Comparar<T>(string campo, T antes, T despues, Func<T, string> mostrar)
            {
                if (!EqualityComparer<T>.Default.Equals(antes, despues))
                    cambios.Add($"{campo}: {mostrar(antes)} → {mostrar(despues)}");
            }

            Comparar("Nombre", producto.Nombre, datos.Nombre.Trim(), s => s);
            Comparar("Descripción", producto.Descripcion, datos.Descripcion.Trim(), s => string.IsNullOrEmpty(s) ? "(vacía)" : s);
            Comparar("Categoría", producto.Categoria, datos.Categoria, c => c.ToString());
            Comparar("Precio", producto.Precio, datos.Precio, Dinero);
            Comparar("Stock mínimo", producto.StockMinimo, datos.StockMinimo, n => n.ToString());
            if (producto.ImagenRuta != datos.ImagenRuta) cambios.Add("Imagen cambiada");

            producto.Nombre = datos.Nombre.Trim();
            producto.Descripcion = datos.Descripcion.Trim();
            producto.Categoria = datos.Categoria;
            producto.Precio = datos.Precio;
            producto.StockMinimo = datos.StockMinimo;
            producto.ImagenRuta = datos.ImagenRuta;

            if (cambios.Count > 0)
                db.Movimientos.Add(Movimiento(TipoMovimiento.EdicionProducto, usuarioId, producto, null, null,
                    Recortar(string.Join("; ", cambios))));

            // La existencia solo se toca si el usuario la cambió. Así una edición de precio no pisa
            // las ventas que se hicieron mientras el formulario estaba abierto.
            if (datos.Stock != stockOriginal)
            {
                if (producto.Stock != stockOriginal)
                    throw new NegocioException(
                        $"La existencia cambió mientras editabas (ahora hay {producto.Stock}). Cierra y vuelve a abrir el producto.");

                db.Movimientos.Add(Movimiento(TipoMovimiento.AjusteStock, usuarioId, producto, stockOriginal, datos.Stock,
                    $"Ajuste manual de existencia: {stockOriginal} → {datos.Stock}"));
                producto.Stock = datos.Stock;
            }

            db.SaveChanges();
            tx.Commit();
        }

        /// <summary>Baja lógica: el producto desaparece del catálogo pero se conserva el historial de ventas.</summary>
        public void Eliminar(int id, int usuarioId)
        {
            using var db = new AppDbContext(App.DbOptions);
            Permisos.ExigirAdministrador(db, usuarioId, "eliminar productos");

            var producto = db.Productos.Find(id);
            if (producto is not { Activo: true })
                throw new NegocioException("El producto ya no existe.");

            producto.Activo = false;
            db.Movimientos.Add(Movimiento(TipoMovimiento.BajaProducto, usuarioId, producto, producto.Stock, producto.Stock,
                $"Baja del catálogo (existencia al momento: {producto.Stock})"));
            db.SaveChanges();
        }

        // ----- Reglas -----

        private static void Validar(ProductoDatos d)
        {
            if (string.IsNullOrWhiteSpace(d.Nombre)) throw new NegocioException("Escribe el nombre del producto.");
            if (d.Nombre.Trim().Length > 100) throw new NegocioException("El nombre es demasiado largo (máximo 100 letras).");
            if (d.Descripcion.Trim().Length > 250) throw new NegocioException("La descripción es demasiado larga (máximo 250 letras).");
            if (d.Precio <= 0) throw new NegocioException("El precio debe ser mayor a cero.");
            if (d.Stock < 0) throw new NegocioException("La existencia no puede ser negativa.");
            if (d.StockMinimo < 0) throw new NegocioException("El stock mínimo no puede ser negativo.");
        }

        private static void ValidarNombreUnico(AppDbContext db, string nombre, int? idActual)
        {
            // En memoria: el lower() de SQLite no entiende acentos (Azúcar / AZÚCAR).
            var existe = db.Productos
                .Where(p => p.Activo && p.Id != idActual)
                .Select(p => p.Nombre)
                .AsEnumerable()
                .Any(n => string.Equals(n, nombre.Trim(), StringComparison.CurrentCultureIgnoreCase));
            if (existe) throw new NegocioException("Ya existe un producto con ese nombre.");
        }

        private static Movimiento Movimiento(TipoMovimiento tipo, int usuarioId, Producto p,
            int? stockAnterior, int? stockNuevo, string detalle) => new()
        {
            Tipo = tipo,
            UsuarioId = usuarioId,
            ProductoId = p.Id,
            ProductoNombre = p.Nombre,
            StockAnterior = stockAnterior,
            StockNuevo = stockNuevo,
            Detalle = detalle,
        };

        private static string Dinero(decimal monto) => monto.ToString("C2", Cultura);

        private static string Recortar(string texto) =>
            texto.Length <= MaxDetalle ? texto : texto[..(MaxDetalle - 1)] + "…";
    }
}

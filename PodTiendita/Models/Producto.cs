namespace PodTiendita.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public CategoriaProducto Categoria { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; } = 5;

        /// <summary>Nombre del archivo dentro de DbPaths.CarpetaImagenes. Null = sin imagen.</summary>
        public string? ImagenRuta { get; set; }

        // Baja lógica: el producto no se borra para conservar el historial de ventas.
        public bool Activo { get; set; } = true;

        public bool StockBajo => Stock <= StockMinimo;
        public bool SinStock => Stock <= 0;
    }
}

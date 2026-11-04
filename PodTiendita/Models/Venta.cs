namespace PodTiendita.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public EstadoVenta Estado { get; set; } = EstadoVenta.Pagada;
        public decimal DescuentoPorcentaje { get; set; }
        public decimal Total { get; set; }
        public List<DetalleVenta> Detalles { get; set; } = new();
    }

    public class DetalleVenta
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }
        public int Cantidad { get; set; }

        // Se guarda el precio del momento: si el precio cambia después, la venta no se altera.
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => PrecioUnitario * Cantidad;
    }
}

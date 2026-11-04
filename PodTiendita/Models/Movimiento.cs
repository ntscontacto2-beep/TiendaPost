namespace PodTiendita.Models
{
    /// <summary>Bitácora: quién hizo qué, cuándo y cuál fue el cambio.</summary>
    public class Movimiento
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public TipoMovimiento Tipo { get; set; }

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // Nullable y sin FK estricta a borrar: se conserva el nombre aunque el producto cambie.
        public int? ProductoId { get; set; }
        public string ProductoNombre { get; set; } = "";

        public int? StockAnterior { get; set; }
        public int? StockNuevo { get; set; }

        /// <summary>Texto legible del cambio, ej. "Precio: $18.00 → $20.00".</summary>
        public string Detalle { get; set; } = "";
    }
}

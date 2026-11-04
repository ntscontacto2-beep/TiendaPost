namespace PodTiendita.Models
{
    public enum Rol
    {
        Administrador = 1,
        Cajero = 2,
    }

    public enum CategoriaProducto
    {
        Abarrotes = 1,
        Licores = 2,
    }

    public enum MetodoPago
    {
        Efectivo = 1,
        Tarjeta = 2,
    }

    public enum EstadoVenta
    {
        Pagada = 1,
        Cancelada = 2,
    }

    public enum TipoMovimiento
    {
        AltaProducto = 1,
        EdicionProducto = 2,
        BajaProducto = 3,
        AjusteStock = 4,
        Venta = 5,
        CancelacionVenta = 6,
    }
}

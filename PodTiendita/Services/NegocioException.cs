namespace PodTiendita.Services
{
    /// <summary>Error de regla de negocio cuyo mensaje se puede mostrar tal cual al usuario.</summary>
    public class NegocioException : Exception
    {
        public NegocioException(string mensaje) : base(mensaje) { }
    }
}

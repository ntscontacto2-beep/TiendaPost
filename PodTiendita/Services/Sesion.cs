using PodTiendita.Models;

namespace PodTiendita.Services
{
    /// <summary>Usuario que tiene la sesión abierta.</summary>
    public static class Sesion
    {
        public static Usuario? Actual { get; private set; }

        public static bool EsAdministrador => Actual?.Rol == Rol.Administrador;

        public static void Iniciar(Usuario usuario) => Actual = usuario;

        public static void Cerrar() => Actual = null;
    }
}

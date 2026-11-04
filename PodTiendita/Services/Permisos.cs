using PodTiendita.Data;
using PodTiendita.Models;

namespace PodTiendita.Services
{
    internal static class Permisos
    {
        /// <summary>Valida contra la BD (no contra la pantalla) que el usuario sea un administrador activo.</summary>
        public static void ExigirAdministrador(AppDbContext db, int usuarioId, string accion)
        {
            var usuario = db.Usuarios.Find(usuarioId);
            if (usuario is not { Activo: true, Rol: Rol.Administrador })
                throw new NegocioException($"Solo el administrador puede {accion}.");
        }
    }
}

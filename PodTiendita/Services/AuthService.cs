using Microsoft.EntityFrameworkCore;
using PodTiendita.Data;
using PodTiendita.Models;

namespace PodTiendita.Services
{
    public class AuthService
    {
        /// <summary>Devuelve el usuario si las credenciales son válidas y está activo; si no, null.</summary>
        public Usuario? Autenticar(string nombreUsuario, string password)
        {
            using var db = new AppDbContext(App.DbOptions);
            var usuario = db.Usuarios.AsNoTracking()
                .FirstOrDefault(u => u.NombreUsuario == nombreUsuario.Trim() && u.Activo);

            return usuario is not null && PasswordHasher.Verificar(password, usuario.PasswordHash)
                ? usuario
                : null;
        }
    }
}

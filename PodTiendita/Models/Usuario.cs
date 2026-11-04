namespace PodTiendita.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public Rol Rol { get; set; } = Rol.Cajero;
        public bool Activo { get; set; } = true;

        public bool EsAdministrador => Rol == Rol.Administrador;
    }
}

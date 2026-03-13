namespace ApiSGA.Models
{
    public class Usuario
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Contrasena { get; set; } = null!;
        public string Rol { get; set; } = null!;
    }
}

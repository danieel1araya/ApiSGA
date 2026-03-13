namespace ApiSGA.Models
{
    public class Sede
    {
        public int Id_Sede { get; set; }
        public string Nombre_Sede { get; set; } = null!;

        // Navegación
        public ICollection<Oficina>? Oficinas { get; set; }
    }
}

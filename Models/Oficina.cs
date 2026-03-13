namespace ApiSGA.Models
{
    public class Oficina
    {
        public int Id_Oficina { get; set; }
        public string Nombre_Oficina { get; set; } = null!;
        public string Encargado { get; set; } = null!;

        // Foreign keys
        public int Id_Sede { get; set; }
        public int Id_Categoria { get; set; }

        // Propiedades de navegación
        public Sede? Sede { get; set; }
        public CategoriaOficina? Categoria { get; set; }
    }
}

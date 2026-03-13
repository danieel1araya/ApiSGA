namespace ApiSGA.Models
{
    public class ActivoUpdateDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int Id_Sede { get; set; }
        public int Id_Oficina { get; set; }
        public string? Observaciones { get; set; }
        public string? Estado_Oaf { get; set; }
    }
}


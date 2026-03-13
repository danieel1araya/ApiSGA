namespace ApiSGA.Models
{
    public class ActivoCreateDto
    {
        public string Placa { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        
    }

}

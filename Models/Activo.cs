using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiSGA.Models
{
    public class Activo
    {
        public int Id_Activo { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int Id_Sede { get; set; }
        public int Id_Oficina { get; set; }
        public string? Observaciones { get; set; }
        public string? Estado_Oaf { get; set; }

        // Navegación (opcional pero recomendado)
        public Sede? Sede { get; set; }
        public Oficina? Oficina { get; set; }
    }
}

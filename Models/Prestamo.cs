using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiSGA.Models
{
    public class Prestamo
    {
        [Key]
        public int Id_Prestamo { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Estado { get; set; } = "PENDIENTE";

        [MaxLength(300)]
        public string Observacion { get; set; } = string.Empty;

        public int Id_Activo { get; set; }
        public int Id_Solicitante { get; set; }
        public int Id_Encargado { get; set; }

        [ForeignKey(nameof(Id_Activo))]
        public Activo Activo { get; set; } = null!;
    }
}

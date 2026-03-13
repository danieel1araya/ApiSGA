namespace ApiSGA.Models
{
    public class Bitacora
    {
        public int Id_Bitacora { get; set; }
        public DateTime Fecha { get; set; }          // DATETIME2
        public string Accion { get; set; } = string.Empty;   
        public string Detalle { get; set; } = string.Empty;  // Texto descriptivo
        public int Id_Usuario { get; set; }

        // Navegación
        public Usuario? Usuario { get; set; }
    }
}

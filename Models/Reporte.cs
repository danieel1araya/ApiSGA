namespace ApiSGA.Models
{
    public class Reporte
    {
        public int Id_Reporte { get; set; }
        public DateTime Fecha { get; set; }          
        public string Tipo_Reporte { get; set; } = string.Empty;
        public int Id_Usuario { get; set; }

        // Navegación opcional hacia Usuario
        public Usuario? Usuario { get; set; }
    }
}

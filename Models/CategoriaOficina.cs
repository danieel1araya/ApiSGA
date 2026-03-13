using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiSGA.Models
{
    [Table("categoria_oficina")]  // 👈 nombre exacto de la tabla en SQL
    public class CategoriaOficina
    {
        [Key]
        [Column("id_categoria")]    // 👈 nombre exacto de la columna
        public int Id_Categoria { get; set; }

        [Column("nombre_categoria")]
        public string Nombre_Categoria { get; set; } = null!;
        // Navegación
        public ICollection<Oficina>? Oficinas { get; set; }
    }
}

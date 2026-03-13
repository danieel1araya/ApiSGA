using ApiSGA.Data;
using ApiSGA.Models;
using ApiSGA.Services; // <-- IMPORTANTE para BitacoraService
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiSGA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OficinaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BitacoraService _bitacora; // <-- servicio

        public OficinaController(AppDbContext context, BitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // ================
        // Helpers
        // ================
        private async Task<Usuario?> GetUsuario(int idUsuario)
        {
            return await _context.Usuarios.FindAsync(idUsuario);
        }

        private bool EsAdmin(Usuario u) => u.Rol == "Administrador";

        // ============================
        //  DTOs
        // ============================
        public class SedeDto
        {
            public int Id_Sede { get; set; }
            public string Nombre_Sede { get; set; } = string.Empty;
        }

        public class OficinaDto
        {
            public int Id_Oficina { get; set; }
            public string Nombre_Oficina { get; set; } = string.Empty;
            public string Encargado { get; set; } = string.Empty;
        }

        public class OficinasPorCategoriaDto
        {
            public int Id_Categoria { get; set; }
            public string Nombre_Categoria { get; set; } = string.Empty;
            public List<OficinaDto> Oficinas { get; set; } = new();
        }

        public class ActualizarEncargadoDto
        {
            public string Encargado { get; set; } = string.Empty;
        }

        // ============================
        //  GET: api/Oficina/Sedes
        // ============================
        [HttpGet("Sedes")]
        public async Task<ActionResult<IEnumerable<SedeDto>>> GetSedes()
        {
            var sedes = await _context.Sedes
                .Select(s => new SedeDto
                {
                    Id_Sede = s.Id_Sede,
                    Nombre_Sede = s.Nombre_Sede
                })
                .ToListAsync();

            return Ok(sedes);
        }

        // ======================================================
        //  GET: api/Oficina/PorSedeAgrupado/{idSede}
        //  Oficinas de una sede, agrupadas por categoría
        // ======================================================
        [HttpGet("PorSedeAgrupado/{idSede:int}")]
        public async Task<ActionResult<IEnumerable<OficinasPorCategoriaDto>>> GetOficinasPorSedeAgrupado(int idSede)
        {
            try
            {
                // 1) Verificar que la sede exista
                var existeSede = await _context.Sedes.AnyAsync(s => s.Id_Sede == idSede);
                if (!existeSede)
                {
                    return NotFound(new { mensaje = "La sede indicada no existe." });
                }

                // 2) JOIN explícito: oficina + categoria_oficina
                var resultado = await (
                    from o in _context.Oficinas
                    join c in _context.Categorias
                        on o.Id_Categoria equals c.Id_Categoria into gj
                    from c in gj.DefaultIfEmpty()    // LEFT JOIN por si alguna oficina no tiene categoría
                    where o.Id_Sede == idSede
                    select new
                    {
                        Oficina = o,
                        Categoria = c
                    }
                ).ToListAsync();

                // 3) Agrupar en memoria por categoría
                var grupos = resultado
                    .GroupBy(x => new
                    {
                        IdCat = x.Categoria != null ? x.Categoria.Id_Categoria : 0,
                        NombreCat = x.Categoria != null ? x.Categoria.Nombre_Categoria : "Sin categoría"
                    })
                    .Select(g => new OficinasPorCategoriaDto
                    {
                        Id_Categoria = g.Key.IdCat,
                        Nombre_Categoria = g.Key.NombreCat,
                        Oficinas = g.Select(x => new OficinaDto
                        {
                            Id_Oficina = x.Oficina.Id_Oficina,
                            Nombre_Oficina = x.Oficina.Nombre_Oficina,
                            Encargado = x.Oficina.Encargado
                        }).ToList()
                    })
                    .OrderBy(g => g.Nombre_Categoria)
                    .ToList();

                return Ok(grupos);
            }
            catch (Exception ex)
            {
                // Mientras estamos montando todo, es útil ver el detalle:
                return StatusCode(500, new
                {
                    mensaje = "Error interno al obtener las oficinas.",
                    detalle = ex.Message
                });
            }
        }

        // ======================================================
        //  PUT: api/Oficina/{idOficina}/Encargado?idUsuario=1
        //  Solo actualizar el encargado (solo ADMIN)
        // ======================================================
        [HttpPut("{idOficina:int}/Encargado")]
        public async Task<IActionResult> ActualizarEncargado(
            int idOficina,
            [FromBody] ActualizarEncargadoDto dto,
            [FromQuery] int idUsuario
        )
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Encargado))
                return BadRequest(new { mensaje = "El encargado no puede estar vacío." });

            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return Unauthorized(new { mensaje = "Usuario inválido." });

            if (!EsAdmin(usuario))
                return Forbid("No tienes permiso para modificar el encargado de una oficina.");

            var oficina = await _context.Oficinas.FindAsync(idOficina);
            if (oficina == null)
                return NotFound(new { mensaje = "Oficina no encontrada." });

            var encargadoAnterior = oficina.Encargado;

            oficina.Encargado = dto.Encargado.Trim();
            await _context.SaveChangesAsync();

            // Bitácora
            string detalle = $"Se actualizó el encargado de la oficina #{oficina.Id_Oficina} " +
                             $"de '{encargadoAnterior}' a '{oficina.Encargado}'.";

            await _bitacora.Log(
                "OFICINA_EDITAR_ENCARGADO",
                detalle,
                usuario.Id_Usuario
            );

            return NoContent();
        }

        // ======================================================
        // GET: api/Oficina/DetalleUbicacion?idSede=1&idOficina=38
        // ======================================================
        [HttpGet("DetalleUbicacion")]
        public async Task<ActionResult<object>> GetDetalleUbicacion(int idSede, int idOficina)
        {
            var sede = await _context.Sedes
                .Where(s => s.Id_Sede == idSede)
                .Select(s => new { s.Id_Sede, s.Nombre_Sede })
                .FirstOrDefaultAsync();

            if (sede == null)
                return NotFound(new { mensaje = "La sede indicada no existe." });

            var oficina = await _context.Oficinas
                .Where(o => o.Id_Oficina == idOficina)
                .Select(o => new { o.Id_Oficina, o.Nombre_Oficina })
                .FirstOrDefaultAsync();

            if (oficina == null)
                return NotFound(new { mensaje = "La oficina indicada no existe." });

            return Ok(new
            {
                sede.Id_Sede,
                sede.Nombre_Sede,
                oficina.Id_Oficina,
                oficina.Nombre_Oficina
            });
        }
    }
}

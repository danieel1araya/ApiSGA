using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiSGA.Data;
using ApiSGA.Models;
using ApiSGA.Services;

namespace ApiSGA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BitacoraService _bitacora;

        public ActivoController(AppDbContext context, BitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // ===========================================================
        // Helper: Verificar permisos
        // ===========================================================
        private async Task<Usuario?> GetUsuario(int idUsuario)
        {
            return await _context.Usuarios.FindAsync(idUsuario);
        }

        private bool EsAdmin(Usuario u) => u.Rol == "Administrador";

        // ===========================================================
        // GET ACTIVOS (sin restricciones)
        // ===========================================================
        [HttpGet]
        public async Task<IActionResult> GetActivos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? placa = null,
            [FromQuery] string? estado = null,
            [FromQuery] int? idSede = null,
            [FromQuery] int? idOficina = null
        )
        {
            var query = _context.Activos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(placa))
                query = query.Where(a => a.Placa.Contains(placa));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(a => a.Estado == estado);

            if (idSede.HasValue)
                query = query.Where(a => a.Id_Sede == idSede.Value);

            if (idOficina.HasValue)
                query = query.Where(a => a.Id_Oficina == idOficina.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(a => a.Id_Activo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                items,
                totalPaginas = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        // ===========================================================
        // GET POR ID (permitido para todos)
        // ===========================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetActivo(int id)
        {
            var a = await _context.Activos.FindAsync(id);
            if (a == null)
                return NotFound();

            return Ok(a);
        }

        // ===========================================================
        // CREATE (solo ADMIN)
        // ===========================================================
        [HttpPost]
        public async Task<IActionResult> CrearActivo([FromBody] Activo nuevo, [FromQuery] int idUsuario)
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario is null)
                return Unauthorized("Usuario inválido.");

            if (!EsAdmin(usuario))
                return Forbid("No tienes permiso para crear activos.");

            _context.Activos.Add(nuevo);
            await _context.SaveChangesAsync();

            // Bitácora
            await _bitacora.Log(
                "ACTIVO_CREAR",
                $"Activo creado: #{nuevo.Id_Activo} (Placa {nuevo.Placa})",
                usuario.Id_Usuario
            );

            return Ok(nuevo);
        }

        // ===========================================================
        // UPDATE (solo ADMIN)
        // ===========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarActivo(int id, [FromBody] Activo datos, [FromQuery] int idUsuario)
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario is null)
                return Unauthorized("Usuario inválido.");

            if (!EsAdmin(usuario))
                return Forbid("No tienes permiso para editar activos.");

            var activo = await _context.Activos.FindAsync(id);
            if (activo == null)
                return NotFound("Activo no encontrado.");

            activo.Descripcion = datos.Descripcion;
            activo.Estado = datos.Estado;
            activo.Id_Sede = datos.Id_Sede;
            activo.Id_Oficina = datos.Id_Oficina;
            activo.Observaciones = datos.Observaciones;
            activo.Estado_Oaf = datos.Estado_Oaf;

            await _context.SaveChangesAsync();

            await _bitacora.Log(
                "ACTIVO_EDITAR",
                $"Activo #{id} fue editado.",
                usuario.Id_Usuario
            );

            return Ok();
        }

        // ===========================================================
        // DELETE (solo ADMIN)
        // ===========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarActivo(int id, [FromQuery] int idUsuario)
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario is null)
                return Unauthorized("Usuario inválido.");

            if (!EsAdmin(usuario))
                return Forbid("No tienes permiso para eliminar activos.");

            var activo = await _context.Activos.FindAsync(id);
            if (activo == null)
                return NotFound("Activo no encontrado.");

            _context.Activos.Remove(activo);
            await _context.SaveChangesAsync();

            await _bitacora.Log(
                "ACTIVO_ELIMINAR",
                $"Activo #{id} eliminado.",
                usuario.Id_Usuario
            );

            return Ok();
        }
    }
}

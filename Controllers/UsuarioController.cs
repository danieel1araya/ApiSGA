using ApiSGA.Data;
using ApiSGA.Models;
using ApiSGA.Services; // <- BitacoraService
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ApiSGA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BitacoraService _bitacora;

        public UsuarioController(AppDbContext context, BitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // =========================
        // HELPERS SEGURIDAD
        // =========================
        private async Task<Usuario?> GetUsuario(int idUsuario)
        {
            return await _context.Usuarios.FindAsync(idUsuario);
        }

        private bool EsAdmin(Usuario u) => u.Rol == "Administrador";

        // =========================
        // GET: api/Usuario?idUsuario=1
        // Solo ADMIN puede listar usuarios
        // =========================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios([FromQuery] int idUsuario)
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return Unauthorized(new { mensaje = "Usuario inválido." });

            if (!EsAdmin(usuario))
                return Forbid("Solo un administrador puede listar usuarios.");

            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .ToListAsync();

            return Ok(usuarios);
        }

        // =========================
        // GET: api/Usuario/5?idUsuario=1
        // Solo ADMIN puede consultar por id
        // =========================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Usuario>> GetUsuarioById(
            int id,
            [FromQuery] int idUsuario)
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return Unauthorized(new { mensaje = "Usuario inválido." });

            if (!EsAdmin(usuario))
                return Forbid("Solo un administrador puede consultar usuarios por id.");

            var usuarioBuscado = await _context.Usuarios.FindAsync(id);

            if (usuarioBuscado == null)
                return NotFound(new { mensaje = "Usuario no encontrado" });

            return Ok(usuarioBuscado);
        }

        // =========================
        // GET: api/Usuario/id-por-correo?correo=...
        // UTILITARIO: lo dejamos sin rol/idUsuario
        // =========================
        [HttpGet("id-por-correo")]
        public async Task<ActionResult<int>> GetIdPorCorreo([FromQuery] string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return BadRequest(new { mensaje = "Debe proporcionar un correo." });

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Correo == correo);

            if (usuario == null)
                return NotFound(new { mensaje = "Usuario no encontrado" });

            return Ok(usuario.Id_Usuario);
        }

        // =========================
        // POST: api/Usuario?idUsuario=1
        // Crear nuevo usuario (solo ADMIN)
        // =========================
        [HttpPost]
        public async Task<ActionResult<Usuario>> CrearUsuario(
            [FromBody] Usuario usuarioNuevo,
            [FromQuery] int idUsuario)
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return Unauthorized(new { mensaje = "Usuario inválido." });

            if (!EsAdmin(usuario))
                return Forbid("Solo un administrador puede crear usuarios.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existeCorreo = await _context.Usuarios
                .AnyAsync(u => u.Correo == usuarioNuevo.Correo);

            if (existeCorreo)
                return Conflict(new { mensaje = "Ya existe un usuario con ese correo." });

            _context.Usuarios.Add(usuarioNuevo);
            await _context.SaveChangesAsync();

            // Bitácora
            await _bitacora.Log(
                "USUARIO_CREAR",
                $"Se creó el usuario #{usuarioNuevo.Id_Usuario} con correo {usuarioNuevo.Correo}.",
                usuario.Id_Usuario
            );

            return CreatedAtAction(nameof(GetUsuarioById),
                new { id = usuarioNuevo.Id_Usuario, idUsuario = idUsuario }, usuarioNuevo);
        }

        // =========================
        // PUT: api/Usuario/5?idUsuario=1
        // Actualizar usuario existente (solo ADMIN)
        // =========================
        [HttpPut("{id:int}")]
        public async Task<ActionResult> ActualizarUsuario(
            int id,
            [FromBody] Usuario usuarioActualizado,
            [FromQuery] int idUsuario)
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return Unauthorized(new { mensaje = "Usuario inválido." });

            if (!EsAdmin(usuario))
                return Forbid("Solo un administrador puede actualizar usuarios.");

            if (id != usuarioActualizado.Id_Usuario)
                return BadRequest(new { mensaje = "El id de la URL no coincide con el del cuerpo." });

            var existe = await _context.Usuarios.AnyAsync(u => u.Id_Usuario == id);
            if (!existe)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            _context.Entry(usuarioActualizado).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                await _bitacora.Log(
                    "USUARIO_EDITAR",
                    $"Se actualizó el usuario #{usuarioActualizado.Id_Usuario}.",
                    usuario.Id_Usuario
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { mensaje = "Error al actualizar el usuario." });
            }

            return NoContent();
        }

        // =========================
        // DELETE: api/Usuario/5?idUsuario=1
        // Eliminar usuario (solo ADMIN)
        // =========================
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> EliminarUsuario(
            int id,
            [FromQuery] int idUsuario)
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return Unauthorized(new { mensaje = "Usuario inválido." });

            if (!EsAdmin(usuario))
                return Forbid("Solo un administrador puede eliminar usuarios.");

            var usuarioBorrar = await _context.Usuarios.FindAsync(id);

            if (usuarioBorrar == null)
                return NotFound(new { mensaje = "Usuario no encontrado." });

            _context.Usuarios.Remove(usuarioBorrar);
            await _context.SaveChangesAsync();

            await _bitacora.Log(
                "USUARIO_ELIMINAR",
                $"Se eliminó el usuario #{usuarioBorrar.Id_Usuario} ({usuarioBorrar.Correo}).",
                usuario.Id_Usuario
            );

            return NoContent();
        }
    }
}

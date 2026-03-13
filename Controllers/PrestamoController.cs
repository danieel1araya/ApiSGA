using ApiSGA.Data;
using ApiSGA.Models;
using ApiSGA.Services;   // <-- NECESARIO PARA BITACORA
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiSGA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrestamoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BitacoraService _bitacora;

        public PrestamoController(AppDbContext context, BitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // ============================================
        // HELPERS DE SEGURIDAD
        // ============================================
        private async Task<Usuario?> GetUsuario(int idUsuario)
        {
            return await _context.Usuarios.FindAsync(idUsuario);
        }

        private bool EsAdmin(Usuario u) => u.Rol == "Administrador";

        // ============================================
        // DTOs
        // ============================================
        public class CrearPrestamoDto
        {
            public int Id_Activo { get; set; }
            public int Id_Solicitante { get; set; }
            public string Observacion { get; set; } = string.Empty;
            public int Id_Encargado { get; set; }
        }

        public class PrestamoListadoDto
        {
            public int Id_Prestamo { get; set; }
            public DateTime Fecha { get; set; }
            public string Estado { get; set; } = string.Empty;
            public string Observacion { get; set; } = string.Empty;

            public int Id_Activo { get; set; }
            public string Placa { get; set; } = string.Empty;
            public string Descripcion_Activo { get; set; } = string.Empty;

            public int Id_Solicitante { get; set; }
            public int Id_Encargado { get; set; }

            public DateTime Fecha_Vencimiento { get; set; }
            public int Dias_Restantes { get; set; }
        }

        // ============================================
        // POST: api/Prestamo        (ADMIN)
        // ============================================
        [HttpPost]
        public async Task<ActionResult> CrearPrestamo(
            [FromBody] CrearPrestamoDto dto,
            [FromQuery] int idUsuario
        )
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return Unauthorized("Usuario inválido.");

            if (!EsAdmin(usuario))
                return Forbid("Solo un administrador puede registrar préstamos.");

            // validar activo
            var activo = await _context.Activos.FindAsync(dto.Id_Activo);

            if (activo == null)
                return NotFound(new { mensaje = "El activo no existe." });

            if (activo.Estado != "DISPONIBLE")
                return BadRequest(new
                {
                    mensaje = "El activo NO está disponible.",
                    estadoActual = activo.Estado
                });

            var ahora = DateTime.Now;

            var prestamo = new Prestamo
            {
                Fecha = ahora,
                Estado = "PENDIENTE",
                Observacion = dto.Observacion,
                Id_Activo = dto.Id_Activo,
                Id_Solicitante = dto.Id_Solicitante,
                Id_Encargado = usuario.Id_Usuario
            };

            activo.Estado = "OCUPADO";

            _context.Prestamos.Add(prestamo);
            await _context.SaveChangesAsync();

            await _bitacora.Log(
                "PRESTAMO_CREAR",
                $"Se creó préstamo #{prestamo.Id_Prestamo} para activo {activo.Placa}.",
                usuario.Id_Usuario
            );

            return Ok(new
            {
                mensaje = "Préstamo creado correctamente.",
                prestamo.Id_Prestamo
            });
        }

        // ============================================
        // GET: api/Prestamo   (TODOS)
        // ============================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PrestamoListadoDto>>> GetPrestamos(
            [FromQuery] string? estado = null)
        {
            var ahora = DateTime.Now;

            var query = _context.Prestamos
                .Include(p => p.Activo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                estado = estado.ToUpper();
                query = query.Where(p => p.Estado == estado);
            }

            var prestamos = await query.ToListAsync();

            bool huboCambios = false;

            // marcar morosos automáticamente
            foreach (var p in prestamos)
            {
                var fechaVenc = p.Fecha.AddDays(30);

                if (ahora > fechaVenc && p.Estado == "PENDIENTE")
                {
                    p.Estado = "MOROSO";
                    huboCambios = true;
                }
            }

            if (huboCambios)
                await _context.SaveChangesAsync();

            var lista = prestamos
                .Select(p =>
                {
                    var fechaVenc = p.Fecha.AddDays(30);
                    var diasRest = (int)Math.Ceiling((fechaVenc - ahora).TotalDays);

                    return new PrestamoListadoDto
                    {
                        Id_Prestamo = p.Id_Prestamo,
                        Fecha = p.Fecha,
                        Estado = p.Estado,
                        Observacion = p.Observacion,
                        Id_Activo = p.Id_Activo,
                        Placa = p.Activo?.Placa ?? "",
                        Descripcion_Activo = p.Activo?.Descripcion ?? "",
                        Id_Solicitante = p.Id_Solicitante,
                        Id_Encargado = p.Id_Encargado,
                        Fecha_Vencimiento = fechaVenc,
                        Dias_Restantes = diasRest
                    };
                })
                // si está pendiente, ordenar por más próximo a vencer
                .OrderBy(p =>
                    p.Estado == "PENDIENTE"
                        ? p.Dias_Restantes
                        : int.MaxValue)
                .ThenByDescending(p => p.Fecha)
                .ToList();

            return Ok(lista);
        }

        // ============================================
        // PUT: api/Prestamo/Devolver/5   (ADMIN)
        // ============================================
        [HttpPut("Devolver/{id:int}")]
        public async Task<ActionResult> Devolver(
            int id,
            [FromQuery] int idUsuario
        )
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return Unauthorized("Usuario inválido.");

            if (!EsAdmin(usuario))
                return Forbid("Solo un administrador puede devolver préstamos.");

            var prestamo = await _context.Prestamos.FindAsync(id);

            if (prestamo == null)
                return NotFound(new { mensaje = "Préstamo no encontrado." });

            if (prestamo.Estado == "DEVUELTO")
                return BadRequest(new { mensaje = "Ya estaba devuelto." });

            var activo = await _context.Activos.FindAsync(prestamo.Id_Activo);

            if (activo == null)
                return NotFound(new { mensaje = "Activo no encontrado." });

            prestamo.Estado = "DEVUELTO";
            activo.Estado = "DISPONIBLE";

            await _context.SaveChangesAsync();

            await _bitacora.Log(
                "PRESTAMO_DEVOLVER",
                $"Se devolvió préstamo #{prestamo.Id_Prestamo} y activo {activo.Placa} ahora está DISPONIBLE.",
                usuario.Id_Usuario
            );

            return Ok(new { mensaje = "Préstamo devuelto exitosamente." });
        }

        // ============================================
        // GET: api/Prestamo/5   (TODOS)
        // ============================================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PrestamoListadoDto>> GetPrestamoPorId(int id)
        {
            var ahora = DateTime.Now;

            var p = await _context.Prestamos
                .Include(x => x.Activo)
                .FirstOrDefaultAsync(x => x.Id_Prestamo == id);

            if (p == null)
                return NotFound(new { mensaje = "Préstamo no encontrado." });

            var fechaVenc = p.Fecha.AddDays(30);

            if (ahora > fechaVenc && p.Estado == "PENDIENTE")
            {
                p.Estado = "MOROSO";
                await _context.SaveChangesAsync();
            }

            var dto = new PrestamoListadoDto
            {
                Id_Prestamo = p.Id_Prestamo,
                Fecha = p.Fecha,
                Estado = p.Estado,
                Observacion = p.Observacion,
                Id_Activo = p.Id_Activo,
                Placa = p.Activo?.Placa ?? "",
                Descripcion_Activo = p.Activo?.Descripcion ?? "",
                Id_Solicitante = p.Id_Solicitante,
                Id_Encargado = p.Id_Encargado,
                Fecha_Vencimiento = fechaVenc,
                Dias_Restantes = (int)Math.Ceiling((fechaVenc - ahora).TotalDays)
            };

            return Ok(dto);
        }
    }
}

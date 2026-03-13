// Controllers/BitacoraController.cs
using ApiSGA.Data;
using ApiSGA.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiSGA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BitacoraController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BitacoraController(AppDbContext context)
        {
            _context = context;
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
        // DTOs
        // =========================
        public class BitacoraItemDto
        {
            public int Id_Bitacora { get; set; }
            public DateTime Fecha { get; set; }
            public string Accion { get; set; } = string.Empty;
            public string Detalle { get; set; } = string.Empty;
            public int Id_Usuario { get; set; }
            public string Nombre_Usuario { get; set; } = string.Empty;
        }

        public class BitacoraPagedResultDto
        {
            public int PaginaActual { get; set; }
            public int TotalPaginas { get; set; }
            public int TotalRegistros { get; set; }
            public List<BitacoraItemDto> Items { get; set; } = new();
        }

        // =========================
        // GET: api/Bitacora?idUsuario=1&page=1&pageSize=50&orden=DESC
        // Solo ADMIN puede ver la bitácora
        // =========================
        [HttpGet]
        public async Task<ActionResult<BitacoraPagedResultDto>> GetBitacora(
            [FromQuery] int idUsuario,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string orden = "DESC")
        {
            // 1) Validar usuario y rol
            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return Unauthorized(new { mensaje = "Usuario inválido." });

            if (!EsAdmin(usuario))
                return Forbid("Solo un administrador puede ver la bitácora.");

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 50;

            // 2) Query base (join para traer el nombre del usuario)
            var query = _context.Bitacoras
                .Include(b => b.Usuario)      // asumiendo navegación Bitacora.Usuario
                .AsNoTracking()
                .AsQueryable();

            // 3) Cantidad total
            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            // 4) Orden
            var ord = (orden ?? "DESC").ToUpper();
            if (ord == "ASC")
                query = query.OrderBy(b => b.Fecha);
            else
                query = query.OrderByDescending(b => b.Fecha);

            // 5) Paginación
            var skip = (page - 1) * pageSize;
            var registros = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            // 6) Proyección a DTO
            var items = registros.Select(b => new BitacoraItemDto
            {
                Id_Bitacora = b.Id_Bitacora,
                Fecha = b.Fecha,
                Accion = b.Accion,
                Detalle = b.Detalle,
                Id_Usuario = b.Id_Usuario,
                Nombre_Usuario = b.Usuario != null ? b.Usuario.Nombre : string.Empty
            }).ToList();

            var resultado = new BitacoraPagedResultDto
            {
                PaginaActual = page,
                TotalPaginas = totalPaginas,
                TotalRegistros = totalRegistros,
                Items = items
            };

            return Ok(resultado);
        }
    }
}

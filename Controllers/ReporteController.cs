// Controllers/ReporteController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiSGA.Data;
using ApiSGA.Models;
using ApiSGA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiSGA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReporteController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BitacoraService _bitacora;

        public ReporteController(AppDbContext context, BitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // =========================
        // Helpers de seguridad
        // =========================
        private async Task<Usuario?> GetUsuario(int idUsuario)
        {
            return await _context.Usuarios.FindAsync(idUsuario);
        }

        private bool EsAdmin(Usuario u) => u.Rol == "Administrador";

        // =========================
        // DTOs
        // =========================

        public class ActivosPorSedeDto
        {
            public int Id_Sede { get; set; }
            public string Nombre_Sede { get; set; } = string.Empty;
            public int Total_Activos { get; set; }
        }

        public class ActivosPorOficinaItemDto
        {
            public int Id_Oficina { get; set; }
            public string Nombre_Oficina { get; set; } = string.Empty;
            public int Total_Activos { get; set; }
        }

        public class ActivosPorOficinaPorSedeDto
        {
            public int Id_Sede { get; set; }
            public string Nombre_Sede { get; set; } = string.Empty;
            public List<ActivosPorOficinaItemDto> Oficinas { get; set; } = new();
        }

        // DTO plano para agrupar en memoria
        private class ActivosPorOficinaFlatDto
        {
            public int Id_Sede { get; set; }
            public string Nombre_Sede { get; set; } = string.Empty;
            public int Id_Oficina { get; set; }
            public string Nombre_Oficina { get; set; } = string.Empty;
            public int Total_Activos { get; set; }
        }

        public class ActivosPorEstadoDto
        {
            public string Estado { get; set; } = string.Empty;
            public int Total { get; set; }
        }

        // =========================
        // Helper para registrar reporte + bitácora
        // =========================
        private async Task<IActionResult> RegistrarReporteAsync(string tipoReporte, int idUsuario)
        {
            var usuario = await GetUsuario(idUsuario);
            if (usuario == null)
                return BadRequest(new { mensaje = "El usuario indicado no existe." });

            if (!EsAdmin(usuario))
                return Forbid("Solo los administradores pueden generar reportes.");

            var rep = new Reporte
            {
                Fecha = DateTime.Today,   // DATE
                Tipo_Reporte = tipoReporte,
                Id_Usuario = idUsuario
            };

            _context.Reportes.Add(rep);
            await _context.SaveChangesAsync();

            await _bitacora.Log(
                "REPORTE_GENERAR",
                $"Se generó el reporte: {tipoReporte}.",
                usuario.Id_Usuario
            );

            // Devolvemos Ok solo como bandera, el controlador que llama decide cómo manejarlo
            return Ok();
        }

        // =========================
        // GET: api/Reporte/ActivosPorSede?idUsuario=1
        // =========================
        [HttpGet("ActivosPorSede")]
        public async Task<ActionResult<IEnumerable<ActivosPorSedeDto>>> GetActivosPorSede(
            [FromQuery] int idUsuario)
        {
            var reg = await RegistrarReporteAsync("ACTIVOS_POR_SEDE", idUsuario);

            if (reg is BadRequestObjectResult badReq)
                return BadRequest(badReq.Value);

            if (reg is ForbidResult forbid)
                return Forbid(forbid.AuthenticationSchemes?.FirstOrDefault());

            var query = from a in _context.Activos
                        join s in _context.Sedes on a.Id_Sede equals s.Id_Sede
                        group a by new { s.Id_Sede, s.Nombre_Sede } into g
                        select new ActivosPorSedeDto
                        {
                            Id_Sede = g.Key.Id_Sede,
                            Nombre_Sede = g.Key.Nombre_Sede,
                            Total_Activos = g.Count()
                        };

            var lista = await query
                .OrderBy(x => x.Nombre_Sede)
                .ToListAsync();

            return Ok(lista);
        }

        // =========================
        // GET: api/Reporte/ActivosPorOficina?idUsuario=1
        // =========================
        [HttpGet("ActivosPorOficina")]
        public async Task<ActionResult<IEnumerable<ActivosPorOficinaPorSedeDto>>> GetActivosPorOficina(
            [FromQuery] int idUsuario)
        {
            var reg = await RegistrarReporteAsync("ACTIVOS_POR_OFICINA", idUsuario);

            if (reg is BadRequestObjectResult badReq)
                return BadRequest(badReq.Value);

            if (reg is ForbidResult forbid)
                return Forbid(forbid.AuthenticationSchemes?.FirstOrDefault());

            // Paso 1: agrupar en SQL por sede + oficina
            var planos = await (
                from a in _context.Activos
                join o in _context.Oficinas on a.Id_Oficina equals o.Id_Oficina
                join s in _context.Sedes on a.Id_Sede equals s.Id_Sede
                group a by new
                {
                    s.Id_Sede,
                    s.Nombre_Sede,
                    o.Id_Oficina,
                    o.Nombre_Oficina
                } into g
                select new ActivosPorOficinaFlatDto
                {
                    Id_Sede = g.Key.Id_Sede,
                    Nombre_Sede = g.Key.Nombre_Sede,
                    Id_Oficina = g.Key.Id_Oficina,
                    Nombre_Oficina = g.Key.Nombre_Oficina,
                    Total_Activos = g.Count()
                }
            ).ToListAsync();

            // Paso 2: agrupar en memoria por sede → lista de oficinas
            var resultado = planos
                .GroupBy(x => new { x.Id_Sede, x.Nombre_Sede })
                .Select(g => new ActivosPorOficinaPorSedeDto
                {
                    Id_Sede = g.Key.Id_Sede,
                    Nombre_Sede = g.Key.Nombre_Sede,
                    Oficinas = g
                        .OrderBy(o => o.Nombre_Oficina)
                        .Select(o => new ActivosPorOficinaItemDto
                        {
                            Id_Oficina = o.Id_Oficina,
                            Nombre_Oficina = o.Nombre_Oficina,
                            Total_Activos = o.Total_Activos
                        })
                        .ToList()
                })
                .OrderBy(r => r.Nombre_Sede)
                .ToList();

            return Ok(resultado);
        }

        // =========================
        // GET: api/Reporte/ActivosPorEstado?idUsuario=1
        // =========================
        [HttpGet("ActivosPorEstado")]
        public async Task<ActionResult<IEnumerable<ActivosPorEstadoDto>>> GetActivosPorEstado(
            [FromQuery] int idUsuario)
        {
            var reg = await RegistrarReporteAsync("ACTIVOS_POR_ESTADO", idUsuario);

            if (reg is BadRequestObjectResult badReq)
                return BadRequest(badReq.Value);

            if (reg is ForbidResult forbid)
                return Forbid(forbid.AuthenticationSchemes?.FirstOrDefault());

            var lista = await _context.Activos
                .GroupBy(a => a.Estado)
                .Select(g => new ActivosPorEstadoDto
                {
                    Estado = g.Key,
                    Total = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .ToListAsync();

            return Ok(lista);
        }
    }
}

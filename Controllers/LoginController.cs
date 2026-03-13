using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiSGA.Data;
using ApiSGA.Models;
using ApiSGA.Services; 

namespace ApiSGA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BitacoraService _bitacora; 

        public LoginController(AppDbContext context, BitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto credenciales)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == credenciales.Correo
                                       && u.Contrasena == credenciales.Contrasena);

            if (usuario == null)
            {
                // No registramos en bitácora porque no hay usuario válido (evita violación de FK)
                return Unauthorized(new { exito = false, mensaje = "Credenciales inválidas" });
            }
            // Registrar login correcto
            await _bitacora.Log(
                "LOGIN_OK",
                $"Usuario {usuario.Nombre} inició sesión correctamente.",
                usuario.Id_Usuario
            );

            return Ok(new
            {
                exito = true,
                mensaje = "Login correcto",
                id_usuario = usuario.Id_Usuario,
                nombre = usuario.Nombre,
                rol = usuario.Rol
            });
        }
    }
}

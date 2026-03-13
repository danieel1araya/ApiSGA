using ApiSGA.Data;
using ApiSGA.Models;

namespace ApiSGA.Services
{
    public class BitacoraService
    {
        private readonly AppDbContext _context;

        public BitacoraService(AppDbContext context)
        {
            _context = context;
        }

        public async Task Log(string accion, string detalle, int idUsuario)
        {
            var bit = new Bitacora
            {
                Fecha = DateTime.Now,
                Accion = accion.ToUpper(),
                Detalle = detalle,
                Id_Usuario = idUsuario
            };

            _context.Bitacoras.Add(bit);
            await _context.SaveChangesAsync();
        }
    }
}

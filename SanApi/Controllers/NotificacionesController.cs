using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanApi.Datos;
using SanApi.Dtos;
using System.Security.Claims;

namespace SanApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificacionesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificacionesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Traer todas las notificaciones del usuario
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificacionDto>>> GetNotificaciones()
        {
            var usuarioId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

            return await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId)
                .OrderByDescending(n => n.FechaCreacion)
                .Select(n => new NotificacionDto
                {
                    Id = n.Id,
                    Titulo = n.Titulo,
                    Mensaje = n.Mensaje,
                    Leida = n.Leida,
                    FechaCreacion = n.FechaCreacion
                }).ToListAsync();
        }

        // PUT: Marcar como leída
        [HttpPut("{id}/marcar-leida")]
        public async Task<IActionResult> MarcarComoLeida(Guid id)
        {
            var usuarioId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioId);

            if (notificacion == null) return NotFound();

            notificacion.Leida = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}


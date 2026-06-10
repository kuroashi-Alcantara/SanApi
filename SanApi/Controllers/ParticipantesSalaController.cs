using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanApi.Datos;
using SanApi.Dtos;
using SanApi.Modelos;
using System.Security.Claims;

namespace SanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Toda persona que intente entrar a un San debe estar logueada
    public class ParticipantesSalaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ParticipantesSalaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AgregarParticipante(ParticipanteCrearDto dto)
        {
            // 1. Identificar quién está ejecutando esta acción
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 2. Buscar la sala
            var sala = await _context.Salas.FindAsync(dto.SalaId);
            if (sala == null) return NotFound("La sala no existe.");

            // 3. LA REGLA DE ORO: Validar Privacidad y Permisos
            bool esElCreador = sala.CreadorId.ToString() == usuarioLogueadoId;

            if (!esElCreador)
            {
                // Si la sala es privada y no eres el creador, bloqueado.
                if (!sala.EsPublica)
                {
                    return StatusCode(403, "Esta sala es privada. Solo el creador puede agregar o invitar participantes.");
                }

                // Si es pública, puedes entrar, PERO solo te puedes inscribir a ti mismo
                // (No puedes usar tu token para inscribir a otras personas)
                if (dto.UsuarioId.ToString() != usuarioLogueadoId)
                {
                    return StatusCode(403, "En salas públicas, solo puedes inscribirte a ti mismo.");
                }
            }

            // 4. Validar límite máximo de la sala
            var cantidadActual = await _context.ParticipantesSala.CountAsync(p => p.SalaId == dto.SalaId);
            if (cantidadActual >= sala.CantidadParticipantes)
            {
                return BadRequest("El San ya está lleno, no acepta más participantes.");
            }

            // 5. Validar que el turno que eligió no esté ocupado
            var turnoOcupado = await _context.ParticipantesSala
                .AnyAsync(p => p.SalaId == dto.SalaId && p.NumeroTurno == dto.NumeroTurno);

            if (turnoOcupado)
            {
                return BadRequest($"El turno número {dto.NumeroTurno} ya está ocupado. Elige otro.");
            }

            // 6. Validar que el turno elegido no sea mayor al límite de participantes
            if (dto.NumeroTurno > sala.CantidadParticipantes)
            {
                return BadRequest($"El número de turno no puede ser mayor al límite de la sala ({sala.CantidadParticipantes}).");
            }

            // 7. Si pasó todas las aduanas de seguridad, lo guardamos
            var nuevoParticipante = new ParticipanteSala
            {
                SalaId = dto.SalaId,
                UsuarioId = dto.UsuarioId,
                NumeroTurno = dto.NumeroTurno,
                EstadoParticipacion = EstadoParticipacion.Activo
            };

            _context.ParticipantesSala.Add(nuevoParticipante);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { Mensaje = "Participante agregado exitosamente al San." });
        }
    }
}
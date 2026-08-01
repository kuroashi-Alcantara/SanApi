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

            // 4.5 Validar si la sala permite múltiples turnos para un mismo usuario
            if (!sala.PermitirMultiplesTurnos)
            {
                // Verificamos si este usuario ya tiene al menos un registro en esta sala
                var yaInscrito = await _context.ParticipantesSala
                    .AnyAsync(p => p.SalaId == dto.SalaId && p.UsuarioId == dto.UsuarioId);

                if (yaInscrito)
                {
                    return BadRequest("Este San no permite que un mismo participante tenga más de un turno.");
                }
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

        [HttpGet("sala/{salaId}")]
        public async Task<IActionResult> GetParticipantesPorSala(Guid salaId)
        {
            // 1. Verificamos que la sala exista
            var salaExiste = await _context.Salas.AnyAsync(s => s.Id == salaId);
            if (!salaExiste)
            {
                return NotFound("La sala especificada no existe.");
            }

            // 2. Buscamos los participantes y los mapeamos al DTO de respuesta
            var participantes = await _context.ParticipantesSala
                .Where(p => p.SalaId == salaId)
                .OrderBy(p => p.NumeroTurno) // Ordenados del turno 1 en adelante
                .Select(p => new ParticipanteRespuestaDto
                {
                    Id = p.Id,
                    SalaId = p.SalaId,
                    UsuarioId = p.UsuarioId,
                    NumeroTurno = p.NumeroTurno,
                    EstadoParticipacion = p.EstadoParticipacion
                })
                .ToListAsync();

            // 3. Devolvemos la lista (incluso si está vacía, devolverá un [] que es útil para el frontend)
            return Ok(participantes);
        }

        // PUT: api/ParticipantesSala/{salaId}/aceptar/{usuarioId}
        [HttpPut("{salaId}/aceptar/{usuarioId}")]
        public async Task<IActionResult> AceptarParticipante(Guid salaId, Guid usuarioId)
        {
            // 1. Identificar quién está ejecutando esta acción
            var usuarioLogueadoId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // 👇 Usamos .Include para traer la lista y evitar el error 500
            var sala = await _context.Salas
                .Include(s => s.ParticipantesSalas)
                .FirstOrDefaultAsync(s => s.Id == salaId);

            if (sala == null) return NotFound("La sala no existe.");

            // 2. Seguridad: Solo el creador de la sala puede aceptar participantes
            if (sala.CreadorId.ToString() != usuarioLogueadoId)
            {
                return StatusCode(403, "Solo el creador del San puede aceptar solicitudes.");
            }

            // 3. Buscar la solicitud pendiente dentro de la colección cargada
            var participante = sala.ParticipantesSalas
                .FirstOrDefault(p => p.UsuarioId == usuarioId);

            if (participante == null)
                return NotFound("No se encontró la solicitud de este usuario.");

            if (participante.EstadoParticipacion == EstadoParticipacion.Activo)
                return BadRequest("Este usuario ya es un participante activo.");

            // 4. Validar límite de la sala antes de aceptar
            var cantidadActual = sala.ParticipantesSalas
                .Count(p => p.EstadoParticipacion == EstadoParticipacion.Activo);

            if (cantidadActual >= sala.CantidadParticipantes)
            {
                return BadRequest("El San ya está lleno, no puedes aceptar más participantes.");
            }

            // 5. Asignar automáticamente el siguiente turno disponible
            // Busca el número más alto actual entre los activos; si no hay turnos, empieza en 0 y le suma 1 (sería el 1), 
            // o si ya se hizo tómbola y hay 3, este nuevo usuario recibirá el turno 4 de manera automática.
            int maxTurnoActual = sala.ParticipantesSalas
                .Where(p => p.EstadoParticipacion == EstadoParticipacion.Activo)
                .Max(p => (int?)p.NumeroTurno) ?? 0;

            participante.NumeroTurno = maxTurnoActual + 1;

            // 6. ¡Aceptado! Cambiamos el estado
            participante.EstadoParticipacion = EstadoParticipacion.Activo;

            await _context.SaveChangesAsync();
            return Ok(new { Mensaje = "Participante aceptado y turno asignado exitosamente." });
        }

        // DELETE: api/ParticipantesSala/{salaId}/rechazar/{usuarioId}
        [HttpDelete("{salaId}/rechazar/{usuarioId}")]
        public async Task<IActionResult> RechazarParticipante(Guid salaId, Guid usuarioId)
        {
            var usuarioLogueadoId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var sala = await _context.Salas.FindAsync(salaId);
            if (sala == null) return NotFound("La sala no existe.");

            if (sala.CreadorId.ToString() != usuarioLogueadoId)
            {
                return StatusCode(403, "Solo el creador del San puede rechazar solicitudes.");
            }

            var participante = await _context.ParticipantesSala
                .FirstOrDefaultAsync(p => p.SalaId == salaId && p.UsuarioId == usuarioId);

            if (participante == null)
                return NotFound("No se encontró la solicitud.");

            // Si lo rechaza, lo más limpio para una solicitud es borrar el registro
            _context.ParticipantesSala.Remove(participante);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Solicitud rechazada y eliminada." });
        }

        // POST: api/ParticipantesSala/{salaId}/asignar-turnos
        [HttpPost("{salaId}/asignar-turnos")]
        public async Task<IActionResult> AsignarTurnos(Guid salaId, [FromBody] AsignarTurnosDto dto)
        {
            try
            {
                // 1. Identificar quién ejecuta la acción desde el Token JWT
                var usuarioLogueadoId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                var sala = await _context.Salas
                    .Include(s => s.ParticipantesSalas)
                    .FirstOrDefaultAsync(s => s.Id == salaId);

                if (sala == null) return NotFound(new { success = false, mensaje = "La sala no existe." });

                // 2. Seguridad: Solo el creador de la sala puede asignar o cambiar turnos
                if (sala.CreadorId.ToString() != usuarioLogueadoId)
                {
                    return StatusCode(403, new { success = false, mensaje = "Solo el creador del San puede asignar turnos." });
                }

                // 3. Regla: Solo se permite modificar turnos en fase de Reclutamiento (Estado 1)
                if ((int)sala.Estado != 1)
                {
                    return BadRequest(new { success = false, mensaje = "No se pueden modificar los turnos una vez iniciado el San." });
                }

                // 4. Actualizamos el número de turno para cada participante
                foreach (var item in dto.Turnos)
                {
                    var participante = sala.ParticipantesSalas
                        .FirstOrDefault(p => p.UsuarioId == item.UsuarioId);

                    if (participante != null)
                    {
                        participante.NumeroTurno = item.NumeroTurno;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, mensaje = "Turnos asignados correctamente." });
            }
            catch (Exception ex)
            {
                // 👇 Esto le dirá a tu app exactamente qué línea o valor causó el choque
                return StatusCode(500, new { success = false, mensaje = $"Error en servidor: {ex.Message} | Inner: {ex.InnerException?.Message}" });
            }
        }
    }
}
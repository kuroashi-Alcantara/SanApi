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
    [Authorize]
    public class PeriodosController : Controller
    {
        private readonly AppDbContext _context;

        public PeriodosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("GenerarCalendario/{salaId}")]
        public async Task<IActionResult> GenerarCalendario(Guid salaId)
        {
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Validar la sala
            var sala = await _context.Salas.FindAsync(salaId);
            if (sala == null) return NotFound("La sala no existe.");

            // 2. Solo el creador puede iniciar el San
            if (sala.CreadorId.ToString() != usuarioLogueadoId)
            {
                return StatusCode(403, "Solo el creador de la sala puede iniciar el San y generar el calendario.");
            }

            // 3. Validar que la sala esté en estado de Reclutamiento
            if (sala.Estado != EstadoSala.Reclutamiento)
            {
                return BadRequest("El calendario ya fue generado o la sala no está en fase de reclutamiento.");
            }

            // 4. Obtener todos los participantes ordenados por turno
            var participantes = await _context.ParticipantesSala
                .Where(p => p.SalaId == salaId)
                .OrderBy(p => p.NumeroTurno)
                .ToListAsync();

            if (!participantes.Any())
            {
                return BadRequest("No hay participantes inscritos en esta sala.");
            }

            // 4.5 AUTO-AJUSTE: Si inicias con menos del límite original, la sala se ajusta automáticamente
            if (participantes.Count < sala.CantidadParticipantes)
            {
                sala.CantidadParticipantes = participantes.Count;
            }

            // 5. Generar el calendario
            DateTime fechaCalculada = sala.FechaInicio;
            var periodosNuevos = new List<Periodo>();
            int rondaActual = 1;

            foreach (var participante in participantes)
            {
                // Calcular la fecha de vencimiento según la frecuencia
                if (sala.Frecuencia == FrecuenciaSala.Semanal)
                {
                    fechaCalculada = fechaCalculada.AddDays(7);
                }
                else if (sala.Frecuencia == FrecuenciaSala.Quincenal)
                {
                    fechaCalculada = fechaCalculada.AddDays(15);
                }
                else if (sala.Frecuencia == FrecuenciaSala.Mensual)
                {
                    fechaCalculada = fechaCalculada.AddMonths(1);
                }

                var nuevoPeriodo = new Periodo
                {
                    SalaId = sala.Id,
                    NumeroRonda = rondaActual,
                    FechaVencimiento = fechaCalculada,
                    BeneficiarioId = participante.UsuarioId,
                    EstadoPeriodo = EstadoPeriodo.Pendiente
                };

                periodosNuevos.Add(nuevoPeriodo);
                rondaActual++;
            }

            // 6. Guardar los periodos y cambiar el estado de la sala a "EnCurso"
            _context.Periodos.AddRange(periodosNuevos);
            sala.Estado = EstadoSala.EnCurso;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "Calendario generado exitosamente y San iniciado.",
                TotalRondas = periodosNuevos.Count,
                ParticipantesFinales = sala.CantidadParticipantes
            });
        }

        [HttpGet("sala/{salaId}")]
        public async Task<IActionResult> GetPeriodosPorSala(Guid salaId)
        {
            // 1. Validar si la sala existe
            var salaExiste = await _context.Salas.AnyAsync(s => s.Id == salaId);
            if (!salaExiste)
            {
                return NotFound("La sala especificada no existe.");
            }

            // 2. Buscar los periodos de esa sala ordenados por número de ronda
            var periodos = await _context.Periodos
                .Where(p => p.SalaId == salaId)
                .OrderBy(p => p.NumeroRonda)
                .Select(p => new PeriodoRespuestaDto
                {
                    Id = p.Id,
                    SalaId = p.SalaId,
                    NumeroRonda = p.NumeroRonda,
                    FechaVencimiento = p.FechaVencimiento,
                    BeneficiarioId = p.BeneficiarioId,
                    EstadoPeriodo = p.EstadoPeriodo,
                    FechaDesembolso = p.FechaDesembolso
                })
                .ToListAsync();

            // 3. Devolver la lista de rondas
            return Ok(periodos);
        }

        [HttpPut("Desembolsar/{id}")]
        public async Task<IActionResult> RegistrarDesembolso(Guid id, [FromBody] DesembolsarPeriodoDto dto)
        {
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Buscar el periodo incluyendo la sala para poder validar quién es el creador
            var periodo = await _context.Periodos
                .Include(p => p.Sala)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (periodo == null)
            {
                return NotFound("El periodo especificado no existe.");
            }

            // 2. SEGURIDAD: Validar que quien intenta desembolsar sea el organizador de la sala
            if (periodo.Sala.CreadorId.ToString() != usuarioLogueadoId)
            {
                return StatusCode(403, "Acceso denegado. Solo el organizador del San puede registrar el desembolso y cerrar la ronda.");
            }

            // 3. Validar que no se haya desembolsado previamente
            if (periodo.EstadoPeriodo == EstadoPeriodo.Completado)
            {
                return BadRequest("Este periodo ya ha sido marcado como completado y el dinero fue entregado.");
            }

            // 4. Actualizar los datos para cerrar oficialmente el periodo
            periodo.UrlComprobanteDesembolso = dto.UrlComprobante;
            periodo.EstadoPeriodo = EstadoPeriodo.Completado;
            periodo.FechaDesembolso = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "Desembolso registrado exitosamente. La ronda ha finalizado.",
                PeriodoId = periodo.Id,
                FechaDesembolso = periodo.FechaDesembolso,
                UrlComprobante = periodo.UrlComprobanteDesembolso,
                EstadoActual = periodo.EstadoPeriodo
            });
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("MiPanel")]
        public async Task<IActionResult> ObtenerMiPanel()
        {
            // 1. Identificar al usuario logueado
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioLogueadoId)) return Unauthorized();

            var usuarioId = Guid.Parse(usuarioLogueadoId);
            var panel = new DashboardRespuestaDto();

            // ====================================================================
            // SECCIÓN A: Mis Salas (Sanes donde participo)
            // ====================================================================
            var misParticipaciones = await _context.ParticipantesSala
                .Include(p => p.Sala)
                .Where(p => p.UsuarioId == usuarioId && p.Sala.Estado != EstadoSala.Finalizada)
                .ToListAsync();

            panel.MisSalas = misParticipaciones.Select(p => new SalaResumenDto
            {
                SalaId = p.SalaId,
                NombreSala = p.Sala.NombreSala,
                MontoCuota = p.Sala.MontoCuota,
                Frecuencia = p.Sala.Frecuencia.ToString(),
                MiTurno = p.NumeroTurno
            }).ToList();

            // Extraemos los IDs de las salas para facilitar las siguientes consultas
            var misSalasIds = misParticipaciones.Select(p => p.SalaId).ToList();

            // ====================================================================
            // SECCIÓN B: Pagos Pendientes
            // ====================================================================
            // Buscamos los periodos activos de las salas del usuario donde él NO es el beneficiario
            var periodosActivos = await _context.Periodos
                .Include(p => p.Sala)
                .Where(p => misSalasIds.Contains(p.SalaId)
                         && p.EstadoPeriodo == EstadoPeriodo.Pendiente
                         && p.BeneficiarioId != usuarioId)
                .ToListAsync();

            foreach (var periodo in periodosActivos)
            {
                // Verificamos si el usuario ya subió un comprobante para este periodo específico
                var yaPago = await _context.Transacciones
                    .AnyAsync(t => t.PeriodoId == periodo.Id
                                && t.UsuarioPagadorId == usuarioId
                                && (t.EstadoPago == EstadoPago.Aprobado || t.EstadoPago == EstadoPago.EnRevision));

                // Si no ha pagado o se lo rechazaron, se lo mostramos como pendiente
                if (!yaPago)
                {
                    panel.PagosPendientes.Add(new PagoPendienteDto
                    {
                        PeriodoId = periodo.Id,
                        NombreSala = periodo.Sala.NombreSala,
                        NumeroRonda = periodo.NumeroRonda,
                        MontoAPagar = periodo.Sala.MontoCuota,
                        FechaVencimiento = periodo.FechaVencimiento
                    });
                }
            }

            // ====================================================================
            // SECCIÓN C: Mis Próximos Cobros
            // ====================================================================
            // Buscamos los periodos pendientes donde el usuario SÍ es el beneficiario
            var misCobros = await _context.Periodos
                .Include(p => p.Sala)
                .Where(p => misSalasIds.Contains(p.SalaId)
                         && p.EstadoPeriodo == EstadoPeriodo.Pendiente
                         && p.BeneficiarioId == usuarioId)
                .OrderBy(p => p.FechaVencimiento)
                .ToListAsync();

            panel.ProximosCobros = misCobros.Select(p => new CobroProximoDto
            {
                PeriodoId = p.Id,
                NombreSala = p.Sala.NombreSala,
                NumeroRonda = p.NumeroRonda,
                MontoEstimado = p.Sala.MontoCuota * p.Sala.CantidadParticipantes, // El pozo total
                FechaCobro = p.FechaVencimiento
            }).ToList();

            return Ok(panel);
        }
    }
}

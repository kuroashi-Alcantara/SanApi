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
            // SECCIÓN B: Pagos Pendientes (Ajustada para salas flexibles)
            // ====================================================================
            // Buscamos los periodos de las salas del usuario donde él NO es el beneficiario
            // Y filtramos para que aparezcan si el periodo está Pendiente, O si está Completado pero la sala permite deudas atrasadas
            var periodosEvaluables = await _context.Periodos
                .Include(p => p.Sala)
                .Where(p => misSalasIds.Contains(p.SalaId)
                         && p.BeneficiarioId != usuarioId
                         && (p.EstadoPeriodo == EstadoPeriodo.Pendiente || p.Sala.PermiteDesembolsoAnticipado))
                .ToListAsync();

            foreach (var periodo in periodosEvaluables)
            {
                // Verificamos si el usuario ya tiene un pago registrado y válido
                var yaPago = await _context.Transacciones
                    .AnyAsync(t => t.PeriodoId == periodo.Id
                                && t.UsuarioPagadorId == usuarioId
                                && (t.EstadoPago == EstadoPago.Aprobado || t.EstadoPago == EstadoPago.EnRevision));

                // Si no ha pagado, se analiza si se muestra como pendiente
                if (!yaPago)
                {
                    // Si el periodo ya está completado pero el usuario NO pagó, significa que quedó moroso en una ronda vieja
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


        [HttpGet("PanelOrganizador")]
        public async Task<IActionResult> ObtenerPanelOrganizador()
        {
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioLogueadoId)) return Unauthorized();

            var usuarioId = Guid.Parse(usuarioLogueadoId);
            var panelAdmin = new AdminDashboardRespuestaDto();

            // 1. Salas Propias (Corregido el conteo de participantes)
            var salasPropias = await _context.Salas
                .Where(s => s.CreadorId == usuarioId && s.Estado != EstadoSala.Finalizada)
                .Select(s => new SalaAdministradaDto
                {
                    SalaId = s.Id,
                    NombreSala = s.NombreSala, // CORREGIDO
                    // Buscamos directamente en la tabla ParticipantesSala usando el ID de la sala
                    ParticipantesActivos = _context.ParticipantesSala.Count(p => p.SalaId == s.Id && p.EstadoParticipacion == EstadoParticipacion.Activo),
                    Estado = s.Estado.ToString()
                })
                .ToListAsync();

            panelAdmin.MisSalasOrganizadas = salasPropias;
            var misSalasIds = salasPropias.Select(s => s.SalaId).ToList();

            // 2. Vouchers pendientes por revisar
            panelAdmin.VouchersPorRevisar = await _context.Transacciones
                .Include(t => t.Periodo)
                .ThenInclude(p => p.Sala)
                .Include(t => t.UsuarioPagador)
                .Where(t => misSalasIds.Contains(t.Periodo.SalaId) && t.EstadoPago == EstadoPago.EnRevision)
                .Select(t => new VoucherPendienteDto
                {
                    TransaccionId = t.Id,
                    NombreSala = t.Periodo.Sala.NombreSala, // CORREGIDO
                    NumeroRonda = t.Periodo.NumeroRonda,
                    NombrePagador = t.UsuarioPagador.NombreCompleto, // CORREGIDO
                    Monto = t.Monto,
                    UrlVoucher = t.UrlVoucher
                })
                .ToListAsync();

            // 3. Desembolsos por hacer
            var periodosDeMisSalas = await _context.Periodos
                .Include(p => p.Sala)
                .Include(p => p.Beneficiario)
                .Where(p => misSalasIds.Contains(p.SalaId) && p.EstadoPeriodo == EstadoPeriodo.Pendiente)
                .ToListAsync();

            foreach (var periodo in periodosDeMisSalas)
            {
                var pagosAprobados = await _context.Transacciones
                    .CountAsync(t => t.PeriodoId == periodo.Id && t.EstadoPago == EstadoPago.Aprobado);

                if (pagosAprobados == periodo.Sala.CantidadParticipantes)
                {
                    panelAdmin.DesembolsosPorHacer.Add(new DesembolsoPendienteDto
                    {
                        PeriodoId = periodo.Id,
                        NombreSala = periodo.Sala.NombreSala, // CORREGIDO
                        NumeroRonda = periodo.NumeroRonda,
                        NombreBeneficiario = periodo.Beneficiario.NombreCompleto, // CORREGIDO
                        MontoTotalPozo = periodo.Sala.MontoCuota * periodo.Sala.CantidadParticipantes
                    });
                }
            }

            return Ok(panelAdmin);
        }
    }
}

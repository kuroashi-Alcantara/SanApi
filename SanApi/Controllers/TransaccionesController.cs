using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
    public class TransaccionesController : ControllerBase // OPTIMIZADO: Cambiado a ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TransaccionesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarPago([FromForm] TransaccionCrearDto dto)
        {
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Validar que el usuario no intente pagar a nombre de otro
            if (usuarioLogueadoId != dto.UsuarioPagadorId.ToString())
            {
                return StatusCode(403, "Seguridad: No puedes registrar un pago a nombre de otro usuario.");
            }

            // 2. Buscar el periodo y traer la información de la sala con sus reglas de negocio
            var periodo = await _context.Periodos
                .Include(p => p.Sala)
                .FirstOrDefaultAsync(p => p.Id == dto.PeriodoId);

            if (periodo == null)
            {
                return NotFound("El periodo especificado no existe.");
            }

            // 3. CORREGIDO: Validar el estado del periodo respetando si la sala es flexible
            if (periodo.EstadoPeriodo == EstadoPeriodo.Completado && !periodo.Sala.PermiteDesembolsoAnticipado)
            {
                return BadRequest("No se pueden registrar pagos para un periodo que ya está cerrado y completado en una sala estricta.");
            }

            // 4. Validar que el usuario realmente pertenezca a la sala de este periodo
            var esParticipante = await _context.ParticipantesSala
                .AnyAsync(p => p.SalaId == periodo.SalaId && p.UsuarioId == dto.UsuarioPagadorId);

            if (!esParticipante)
            {
                return StatusCode(403, "El usuario no es participante de la sala a la que pertenece este cobro.");
            }

            // =========================================================
            // NUEVA VALIDACIÓN: EVITAR PAGOS DUPLICADOS
            // =========================================================
            var yaTienePagoActivo = await _context.Transacciones
                .AnyAsync(t => t.PeriodoId == dto.PeriodoId
                            && t.UsuarioPagadorId == dto.UsuarioPagadorId
                            && (t.EstadoPago == EstadoPago.Aprobado || t.EstadoPago == EstadoPago.EnRevision));

            if (yaTienePagoActivo)
            {
                return BadRequest("Ya tienes un pago registrado o en revisión para esta ronda.");
            }
            // =========================================================

            // 4.5 Candado de Secuencia: Validar que no tenga pagos atrasados en rondas anteriores
            var rondasAnterioresIds = await _context.Periodos
                .Where(p => p.SalaId == periodo.SalaId && p.NumeroRonda < periodo.NumeroRonda)
                .Select(p => p.Id)
                .ToListAsync();

            if (rondasAnterioresIds.Any())
            {
                var rondasPagadas = await _context.Transacciones
                    .Where(t => rondasAnterioresIds.Contains(t.PeriodoId)
                             && t.UsuarioPagadorId == dto.UsuarioPagadorId
                             && (t.EstadoPago == EstadoPago.Aprobado || t.EstadoPago == EstadoPago.EnRevision))
                    .Select(t => t.PeriodoId)
                    .Distinct()
                    .CountAsync();

                if (rondasPagadas < rondasAnterioresIds.Count)
                {
                    return BadRequest($"Orden incorrecto: No puedes abonar a la Ronda {periodo.NumeroRonda} porque tienes deudas pendientes en rondas anteriores.");
                }
            }

            // =========================================================
            // LÓGICA DE GUARDADO FÍSICO DE LA IMAGEN
            // =========================================================
            string carpetaDestino = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "vouchers");

            if (!Directory.Exists(carpetaDestino))
            {
                Directory.CreateDirectory(carpetaDestino);
            }

            string nombreArchivoUnico = Guid.NewGuid().ToString() + Path.GetExtension(dto.ImagenVoucher.FileName);
            string rutaFisicaCompleta = Path.Combine(carpetaDestino, nombreArchivoUnico);

            using (var fileStream = new FileStream(rutaFisicaCompleta, FileMode.Create))
            {
                await dto.ImagenVoucher.CopyToAsync(fileStream);
            }

            string urlBase = $"{Request.Scheme}://{Request.Host}";
            string urlBD = $"{urlBase}/vouchers/{nombreArchivoUnico}";
            // =========================================================

            var nuevaTransaccion = new Transaccion
            {
                PeriodoId = dto.PeriodoId,
                UsuarioPagadorId = dto.UsuarioPagadorId,
                Monto = dto.Monto,
                UrlVoucher = urlBD,
                EstadoPago = EstadoPago.EnRevision
            };

            _context.Transacciones.Add(nuevaTransaccion);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Pago registrado exitosamente. En espera de revisión.", TransaccionId = nuevaTransaccion.Id });
        }

        [HttpPut("Evaluar/{id}")]
        public async Task<IActionResult> EvaluarPago(Guid id, [FromBody] EvaluarTransaccionDto dto)
        {
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (dto.Estado == EstadoPago.EnRevision)
            {
                return BadRequest("El estado debe ser Aprobado o Rechazado.");
            }

            var transaccion = await _context.Transacciones
                .Include(t => t.Periodo)
                .ThenInclude(p => p.Sala)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaccion == null)
            {
                return NotFound("La transacción especificada no existe.");
            }

            if (transaccion.Periodo.Sala.CreadorId.ToString() != usuarioLogueadoId)
            {
                return StatusCode(403, "Acceso denegado. Solo el organizador del San puede aprobar o rechazar pagos.");
            }

            if (transaccion.EstadoPago != EstadoPago.EnRevision)
            {
                return BadRequest($"Esta transacción ya fue evaluada previamente con el estado: {transaccion.EstadoPago}.");
            }

            transaccion.EstadoPago = dto.Estado;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = $"El pago ha sido {transaccion.EstadoPago.ToString().ToLower()} exitosamente.",
                TransaccionId = transaccion.Id,
                NuevoEstado = transaccion.EstadoPago
            });
        }

        [HttpGet("periodo/{periodoId}")]
        public async Task<IActionResult> ObtenerTransaccionesPorPeriodo(Guid periodoId)
        {
            var periodoExiste = await _context.Periodos.AnyAsync(p => p.Id == periodoId);
            if (!periodoExiste)
            {
                return NotFound("El periodo especificado no existe.");
            }

            var transacciones = await _context.Transacciones
                .Where(t => t.PeriodoId == periodoId)
                .Select(t => new TransaccionRespuestaDto
                {
                    Id = t.Id,
                    PeriodoId = t.PeriodoId,
                    UsuarioPagadorId = t.UsuarioPagadorId,
                    Monto = t.Monto,
                    UrlVoucher = t.UrlVoucher,
                    EstadoPago = t.EstadoPago
                })
                .ToListAsync();

            return Ok(transacciones);
        }
    }
}
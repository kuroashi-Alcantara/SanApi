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
    public class TransaccionesController : Controller
    {
        private readonly AppDbContext _context;

        public TransaccionesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarPago([FromBody] TransaccionCrearDto dto)
        {
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Validar que el usuario no intente pagar a nombre de otro
            if (usuarioLogueadoId != dto.UsuarioPagadorId.ToString())
            {
                return StatusCode(403, "Seguridad: No puedes registrar un pago a nombre de otro usuario.");
            }

            // 2. Buscar el periodo y traer la información básica de la sala
            var periodo = await _context.Periodos
                .Include(p => p.Sala)
                .FirstOrDefaultAsync(p => p.Id == dto.PeriodoId);

            if (periodo == null)
            {
                return NotFound("El periodo especificado no existe.");
            }

            // 3. Validar que el periodo esté activo para recibir pagos
            if (periodo.EstadoPeriodo == EstadoPeriodo.Completado)
            {
                return BadRequest("No se pueden registrar pagos para un periodo que ya está cerrado y completado.");
            }

            // 4. Validar que el usuario realmente pertenezca a la sala de este periodo
            var esParticipante = await _context.ParticipantesSala
                .AnyAsync(p => p.SalaId == periodo.SalaId && p.UsuarioId == dto.UsuarioPagadorId);

            if (!esParticipante)
            {
                return StatusCode(403, "El usuario no es participante de la sala a la que pertenece este cobro.");
            }

            // 5. Crear el registro en la base de datos
            var nuevaTransaccion = new Transaccion
            {
                PeriodoId = dto.PeriodoId,
                UsuarioPagadorId = dto.UsuarioPagadorId,
                Monto = dto.Monto,
                UrlVoucher = dto.UrlVoucher,
                EstadoPago = EstadoPago.EnRevision // Todo pago entra en revisión por defecto
            };

            _context.Transacciones.Add(nuevaTransaccion);
            await _context.SaveChangesAsync();

            // 6. Preparar la respuesta usando el DTO de salida
            var respuesta = new TransaccionRespuestaDto
            {
                Id = nuevaTransaccion.Id,
                PeriodoId = nuevaTransaccion.PeriodoId,
                UsuarioPagadorId = nuevaTransaccion.UsuarioPagadorId,
                Monto = nuevaTransaccion.Monto,
                UrlVoucher = nuevaTransaccion.UrlVoucher,
                EstadoPago = nuevaTransaccion.EstadoPago
            };

            return Ok(new
            {
                Mensaje = "Pago registrado exitosamente y en espera de revisión del organizador.",
                Transaccion = respuesta
            });
        }

        [HttpPut("Evaluar/{id}")]
        public async Task<IActionResult> EvaluarPago(Guid id, [FromBody] EvaluarTransaccionDto dto)
        {
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Validar que la decisión sea válida (No puede volver a ponerlo en revisión)
            if (dto.Estado == EstadoPago.EnRevision)
            {
                return BadRequest("El estado debe ser Aprobado o Rechazado.");
            }

            // 2. Buscar la transacción incluyendo el Periodo y la Sala para validar al creador
            var transaccion = await _context.Transacciones
                .Include(t => t.Periodo)
                .ThenInclude(p => p.Sala)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaccion == null)
            {
                return NotFound("La transacción especificada no existe.");
            }

            // 3. SEGURIDAD: Validar que el usuario logueado sea el dueño/creador de la sala
            if (transaccion.Periodo.Sala.CreadorId.ToString() != usuarioLogueadoId)
            {
                return StatusCode(403, "Acceso denegado. Solo el organizador del San puede aprobar o rechazar pagos.");
            }

            // 4. Validar que la transacción no haya sido procesada antes para evitar duplicidad
            if (transaccion.EstadoPago != EstadoPago.EnRevision)
            {
                return BadRequest($"Esta transacción ya fue evaluada previamente con el estado: {transaccion.EstadoPago}.");
            }

            // 5. Aplicar el nuevo estado
            transaccion.EstadoPago = dto.Estado;

            // Aquí guardamos el cambio de estado del pago
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
            // 1. Validar que el periodo exista
            var periodoExiste = await _context.Periodos.AnyAsync(p => p.Id == periodoId);
            if (!periodoExiste)
            {
                return NotFound("El periodo especificado no existe.");
            }

            // 2. Traer todas las transacciones de ese periodo y mapearlas al DTO
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

            // 3. Devolvemos la lista (si nadie ha pagado aún, devolverá un arreglo vacío [])
            return Ok(transacciones);
        }
    }
}

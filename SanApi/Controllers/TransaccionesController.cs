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
    }
}

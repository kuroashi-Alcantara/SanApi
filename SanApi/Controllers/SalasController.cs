using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanApi.Datos;
using SanApi.Dtos;
using SanApi.Modelos; 
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SanApi.Controllers
{
    [Authorize] // Protege TODOS los métodos de este controlador
    [Route("api/[controller]")]
    [ApiController]
    public class SalasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalasController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Salas
        [HttpPost]
        public async Task<IActionResult> CrearSala(SalaCrearDto dto)
        {
            // 1. MAGIA DEL JWT: Extraemos el ID del usuario logueado directamente del token
            // .NET mapea el 'Sub' que configuramos en el Login a 'NameIdentifier'
            var creadorIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(creadorIdString) || !Guid.TryParse(creadorIdString, out Guid creadorId))
            {
                return Unauthorized("No se pudo identificar al usuario desde el token.");
            }

            // 2. Mapeo Manual: De DTO a Entidad
            var nuevaSala = new Sala
            {
                // El Id se genera automáticamente por el Guid.NewGuid() de tu modelo
                CreadorId = creadorId, // Asignado de forma 100% segura
                NombreSala = dto.NombreSala,
                MontoCuota = dto.MontoCuota,
                Frecuencia = dto.Frecuencia,
                CantidadParticipantes = dto.CantidadParticipantes,
                EsPublica = dto.EsPublica,
                PermitirMultiplesTurnos = dto.PermitirMultiplesTurnos,
                PermiteDesembolsoAnticipado = dto.PermiteDesembolsoAnticipado,
                FechaInicio = dto.FechaInicio
                // Estado y FechaCreacion ya toman sus valores por defecto (1 y DateTime.UtcNow)
            };

            // 3. Guardar en la base de datos
            _context.Salas.Add(nuevaSala);
            await _context.SaveChangesAsync();

            // 4. Mapeo Manual: De Entidad a DTO de Respuesta
            var respuesta = new SalaRespuestaDto
            {
                Id = nuevaSala.Id,
                CreadorId = nuevaSala.CreadorId,
                NombreSala = nuevaSala.NombreSala,
                MontoCuota = nuevaSala.MontoCuota,
                Frecuencia = nuevaSala.Frecuencia,
                CantidadParticipantes = nuevaSala.CantidadParticipantes,
                EsPublica = nuevaSala.EsPublica,
                PermitirMultiplesTurnos = nuevaSala.PermitirMultiplesTurnos,
                PermiteDesembolsoAnticipado = nuevaSala.PermiteDesembolsoAnticipado,
                Estado = nuevaSala.Estado,
                FechaInicio = nuevaSala.FechaInicio,
                FechaCreacion = nuevaSala.FechaCreacion
            };

            // Devuelve un Código 201 (Created) y apunta al método GetSala para ver el resultado
            return CreatedAtAction(nameof(GetSala), new { id = nuevaSala.Id }, respuesta);
        }

        // GET: api/Salas/{id}
        // Lo creamos rápidamente para que el CreatedAtAction de arriba funcione correctamente
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSala(Guid id)
        {
            var sala = await _context.Salas.FindAsync(id);

            if (sala == null)
            {
                return NotFound("La sala no existe.");
            }

            var respuesta = new SalaRespuestaDto
            {
                Id = sala.Id,
                CreadorId = sala.CreadorId,
                NombreSala = sala.NombreSala,
                MontoCuota = sala.MontoCuota,
                Frecuencia = sala.Frecuencia,
                CantidadParticipantes = sala.CantidadParticipantes,
                EsPublica = sala.EsPublica,
                PermitirMultiplesTurnos = sala.PermitirMultiplesTurnos,
                Estado = sala.Estado,
                FechaInicio = sala.FechaInicio,
                FechaCreacion = sala.FechaCreacion
            };

            return Ok(respuesta);
        }

        // GET: api/Salas
        [HttpGet]
        public async Task<IActionResult> GetTodasLasSalas()
        {
            // Traemos todas las salas y las convertimos al DTO de respuesta
            var salas = await _context.Salas
                .Select(s => new SalaRespuestaDto
                {
                    Id = s.Id,
                    CreadorId = s.CreadorId,
                    NombreSala = s.NombreSala,
                    MontoCuota = s.MontoCuota,
                    Frecuencia = s.Frecuencia,
                    CantidadParticipantes = s.CantidadParticipantes,
                    EsPublica = s.EsPublica,
                    PermitirMultiplesTurnos = s.PermitirMultiplesTurnos,
                    Estado = s.Estado,
                    FechaInicio = s.FechaInicio,
                    FechaCreacion = s.FechaCreacion
                })
                .ToListAsync();

            return Ok(salas);
        }

        // PUT: api/Salas/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarSala(Guid id, SalaActualizarDto dto)
        {
            // 1. Buscamos la sala en la base de datos
            var sala = await _context.Salas.FindAsync(id);

            if (sala == null)
            {
                return NotFound("La sala que intentas modificar no existe.");
            }

            // 2. SEGURIDAD: Extraemos el ID del usuario logueado desde el Token
            var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            // Comparamos si el usuario logueado es el dueño de la sala
            if (sala.CreadorId.ToString() != usuarioIdString)
            {
                // Status 403 Forbid: Sabes quién eres (estás autenticado), pero no tienes permiso para esto
                return StatusCode(403, "No tienes permiso para modificar esta sala. Solo el creador puede hacerlo.");
            }

            // 3. Si pasó la seguridad, actualizamos los datos permitidos
            sala.NombreSala = dto.NombreSala;
            sala.MontoCuota = dto.MontoCuota;
            sala.Frecuencia = dto.Frecuencia;
            sala.CantidadParticipantes = dto.CantidadParticipantes;
            sala.EsPublica = dto.EsPublica;
            sala.PermitirMultiplesTurnos = dto.PermitirMultiplesTurnos;
            sala.Estado = dto.Estado;

            // 4. Guardamos los cambios
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Sala actualizada correctamente." });
        }

        //nuevos endopoijnt para salas
        // GET: api/Salas/administradas
        [HttpGet("administradas")]
        public async Task<IActionResult> GetSalasAdministradas()
        {
            // 1. Obtenemos el ID del usuario del Token
            var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(usuarioIdString) || !Guid.TryParse(usuarioIdString, out Guid usuarioId))
                return Unauthorized();

            // 2. Filtramos donde él sea el creador
            var salas = await _context.Salas
                .Where(s => s.CreadorId == usuarioId)
                .Select(s => new SalaRespuestaDto
                {
                    Id = s.Id,
                    CreadorId = s.CreadorId,
                    NombreSala = s.NombreSala,
                    MontoCuota = s.MontoCuota,
                    Frecuencia = s.Frecuencia,
                    CantidadParticipantes = s.CantidadParticipantes,
                    EsPublica = s.EsPublica,
                    PermitirMultiplesTurnos = s.PermitirMultiplesTurnos,
                    Estado = s.Estado,
                    FechaInicio = s.FechaInicio,
                    FechaCreacion = s.FechaCreacion
                })
                .ToListAsync();

            return Ok(salas);
        }

        // GET: api/Salas/participadas
        [HttpGet("participadas")]
        public async Task<IActionResult> GetSalasParticipadas()
        {
            var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(usuarioIdString) || !Guid.TryParse(usuarioIdString, out Guid usuarioId))
                return Unauthorized();

            // Filtramos usando la tabla intermedia ParticipantesSala
            var salas = await _context.ParticipantesSala
                .Where(p => p.UsuarioId == usuarioId)
                .Include(p => p.Sala) // Traemos los datos de la sala
                .Select(p => new SalaRespuestaDto
                {
                    Id = p.Sala.Id,
                    CreadorId = p.Sala.CreadorId,
                    NombreSala = p.Sala.NombreSala,
                    MontoCuota = p.Sala.MontoCuota,
                    Frecuencia = p.Sala.Frecuencia,
                    CantidadParticipantes = p.Sala.CantidadParticipantes,
                    EsPublica = p.Sala.EsPublica,
                    PermitirMultiplesTurnos = p.Sala.PermitirMultiplesTurnos,
                    Estado = p.Sala.Estado,
                    FechaInicio = p.Sala.FechaInicio,
                    FechaCreacion = p.Sala.FechaCreacion
                })
                .ToListAsync();

            return Ok(salas);
        }

        // POST: api/Salas/unirse/{codigoSala}
        [HttpPost("unirse/{codigoSala}")]
        public async Task<IActionResult> UnirseASala(Guid codigoSala) // Asumiendo que usan el Id como código por ahora
        {
            var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(usuarioIdString) || !Guid.TryParse(usuarioIdString, out Guid usuarioId))
                return Unauthorized();

            var sala = await _context.Salas.FindAsync(codigoSala);
            if (sala == null) return NotFound("La sala no existe.");

            // Validar si ya está en la sala
            var yaExiste = await _context.ParticipantesSala
                .AnyAsync(p => p.SalaId == codigoSala && p.UsuarioId == usuarioId);

            if (yaExiste) return BadRequest("Ya eres participante de esta sala.");

            // Agregar a la tabla intermedia
            var nuevoParticipante = new ParticipanteSala
            {
                SalaId = codigoSala,
                UsuarioId = usuarioId,
                EstadoParticipacion = EstadoParticipacion.Activo // O el enum que utilices
            };

            _context.ParticipantesSala.Add(nuevoParticipante);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Te has unido a la sala exitosamente." });
        }

        [HttpPost("{id}/SortearTurnos")]
        public async Task<IActionResult> EjecutarTombola(Guid id)
        {
            var usuarioLogueadoId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioLogueadoId)) return Unauthorized();

            var usuarioId = Guid.Parse(usuarioLogueadoId);

            // 1. Buscar la sala
            var sala = await _context.Salas.FindAsync(id);
            if (sala == null) return NotFound("La sala no existe.");

            // 2. SEGURIDAD: Solo el creador puede girar la tómbola
            if (sala.CreadorId != usuarioId)
            {
                return StatusCode(403, "Solo el organizador de la sala puede realizar el sorteo de turnos.");
            }

            // 3. Validar el estado de la sala
            if (sala.Estado != EstadoSala.Reclutamiento)
            {
                return BadRequest("El sorteo solo se puede realizar mientras la sala está en fase de reclutamiento.");
            }

            // 4. Validar las reglas de negocio que configuramos
            if (!sala.SorteoTurnosAleatorio)
            {
                return BadRequest("Esta sala está configurada para asignación manual de turnos. No se puede usar la tómbola.");
            }

            // 5. Traer a los participantes activos
            var participantes = await _context.ParticipantesSala
                .Include(p => p.Usuario)
                .Where(p => p.SalaId == id && p.EstadoParticipacion == EstadoParticipacion.Activo)
                .ToListAsync();

            if (participantes.Count == 0)
            {
                return BadRequest("No hay participantes en la sala para realizar el sorteo.");
            }

            // ====================================================================
            // EL ALGORITMO DE LA TÓMBOLA
            // ====================================================================

            // Usamos Guid.NewGuid() para desordenar la lista de forma criptográficamente aleatoria
            var participantesMezclados = participantes.OrderBy(p => Guid.NewGuid()).ToList();

            var resultados = new List<ResultadoSorteoDto>();
            int turnoActual = 1;

            foreach (var participante in participantesMezclados)
            {
                // Asignamos el número
                participante.NumeroTurno = turnoActual;

                // Guardamos para el reporte visual
                resultados.Add(new ResultadoSorteoDto
                {
                    NombreParticipante = participante.Usuario.NombreCompleto,
                    NumeroTurno = turnoActual
                });

                turnoActual++;
            }

            // Guardamos los turnos oficiales en la base de datos
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Mensaje = "¡Sorteo realizado con éxito! Los turnos han sido asignados aleatoriamente.",
                Resultados = resultados.OrderBy(r => r.NumeroTurno) // Devolvemos la lista ordenada del 1 al N para que se vea bonita
            });
        }
    }
}
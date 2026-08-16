using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanApi.Datos;
using SanApi.Dtos;
using SanApi.Modelos; 
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            // Usamos Include para traer la relación Muchos a Muchos
            var sala = await _context.Salas
                .Include(s => s.ParticipantesSalas) // Tu tabla intermedia
                    .ThenInclude(ps => ps.Usuario)  // Traemos los datos del usuario real
                .FirstOrDefaultAsync(s => s.Id == id);

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
                PermiteDesembolsoAnticipado = sala.PermiteDesembolsoAnticipado,
                Estado = sala.Estado,
                FechaInicio = sala.FechaInicio,
                FechaCreacion = sala.FechaCreacion,

                // Mapeamos la lista de participantes dinámicamente
                Participantes = sala.ParticipantesSalas.Select(ps => new ParticipanteSalaDto
                {
                    UsuarioId = ps.UsuarioId,
                    Nombre = ps.Usuario.NombreCompleto, 
                    NumeroTurno = ps.NumeroTurno,
                    EstadoParticipacion = (int)ps.EstadoParticipacion
                }).ToList()
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
            sala.PermiteDesembolsoAnticipado = dto.PermiteDesembolsoAnticipado;
            sala.FechaInicio = dto.FechaInicio;

            // 4. Guardamos los cambios
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Sala actualizada correctamente." });
        }

        
        // GET: api/Salas/administradas
        [HttpGet("administradas")]
        public async Task<IActionResult> GetSalasAdministradas([FromQuery] bool incluirCanceladas = false)
        {
            // 1. Obtenemos el ID del usuario del Token
            var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(usuarioIdString) || !Guid.TryParse(usuarioIdString, out Guid usuarioId))
                return Unauthorized();

            // 2. Armamos la consulta base: Filtramos donde él sea el creador
            var query = _context.Salas.Where(s => s.CreadorId == usuarioId);

            // 3. Aplicamos el filtro de estado de manera dinámica
            if (incluirCanceladas)
            {
                // Trae las vivas y las del historial (Canceladas y Finalizadas)
                query = query.Where(s => s.Estado == EstadoSala.Reclutamiento ||
                                         s.Estado == EstadoSala.EnCurso ||
                                         s.Estado == EstadoSala.Cancelada ||
                                         s.Estado == EstadoSala.Finalizada);
            }
            else
            {
                // Pantalla limpia: Solo trae las vivas
                query = query.Where(s => s.Estado == EstadoSala.Reclutamiento ||
                                         s.Estado == EstadoSala.EnCurso);
            }

            // 4. Mapeamos y ejecutamos
            var salas = await query
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
        public async Task<IActionResult> GetSalasParticipadas([FromQuery] bool incluirCanceladas = false)
        {
            var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(usuarioIdString) || !Guid.TryParse(usuarioIdString, out Guid usuarioId))
                return Unauthorized();

            // 1. Consulta base: Filtramos usando la tabla intermedia ParticipantesSala
            var query = _context.ParticipantesSala
                .Where(p => p.UsuarioId == usuarioId)
                .Include(p => p.Sala)
                .AsQueryable();

            // 2. Filtro dinámico accediendo a las propiedades de p.Sala
            if (incluirCanceladas)
            {
                query = query.Where(p => p.Sala.Estado == EstadoSala.Reclutamiento ||
                                         p.Sala.Estado == EstadoSala.EnCurso ||
                                         p.Sala.Estado == EstadoSala.Cancelada ||
                                         p.Sala.Estado == EstadoSala.Finalizada);
            }
            else
            {
                query = query.Where(p => p.Sala.Estado == EstadoSala.Reclutamiento ||
                                         p.Sala.Estado == EstadoSala.EnCurso);
            }

            // 3. Mapeamos al DTO y ejecutamos
            var salas = await query
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
                    FechaCreacion = p.Sala.FechaCreacion,
                    MiEstadoParticipacion = (int)p.EstadoParticipacion
                })
                .ToListAsync();

            return Ok(salas);
        }

        // POST: api/Salas/unirse/{codigoSala}
        [HttpPost("unirse/{codigoSala}")]
        public async Task<IActionResult> UnirseASala(Guid codigoSala) // usamos el id como codigo por ahora
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
                EstadoParticipacion = EstadoParticipacion.Pendiente
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
                    NumeroTurno = turnoActual,
                    //New
                    EstadoParticipacion = EstadoParticipacion.Activo
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

        //Iniciar San
        [HttpPost("{salaId}/iniciar")]
        public async Task<IActionResult> IniciarSan(Guid salaId)
        {
            var usuarioLogueadoId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var sala = await _context.Salas
                // Nota el cambio aquí: Usamos tu propiedad exacta ParticipantesSalas
                .Include(s => s.ParticipantesSalas)
                .FirstOrDefaultAsync(s => s.Id == salaId);

            if (sala == null) return NotFound("La sala no existe.");

            // 1. Autoridad de Inicio: Solo el Organizador puede detonar la creación del calendario[cite: 1].
            if (sala.CreadorId.ToString() != usuarioLogueadoId)
                return StatusCode(403, "Solo el Organizador puede detonar la creación del calendario.");

            // 2. Validación de Estado: La sala debe estar obligatoriamente en fase de Reclutamiento[cite: 1].
            if (sala.Estado != EstadoSala.Reclutamiento)
                return BadRequest("La sala debe estar obligatoriamente en fase de Reclutamiento para iniciar.");

            // 3. Restricción de Quórum: El sistema abortará la generación si no hay participantes inscritos[cite: 1].
            // (Asegúrate de que EstadoParticipacion siga siendo tu Enum para los miembros)
            var activos = sala.ParticipantesSalas
                .Where(p => p.EstadoParticipacion == EstadoParticipacion.Activo)
                .OrderBy(p => p.NumeroTurno)
                .ToList();

            if (!activos.Any())
                return BadRequest("El sistema abortará la generación si no hay participantes inscritos.");

            // 4. Auto-Ajuste Inteligente: El sistema muta automáticamente la capacidad de la sala para igualarla a los participantes actuales[cite: 1].
            sala.CantidadParticipantes = activos.Count;

            // 5. Proyección Cronológica: El sistema crea un Periodo por cada participante activo, asignando a cada uno como BeneficiarioId[cite: 1].
            DateTime fechaCalculada = sala.FechaInicio;
            var periodosNuevos = new List<Periodo>();

            foreach (var participante in activos)
            {
                var nuevoPeriodo = new Periodo
                {
                    Id = Guid.NewGuid(),
                    SalaId = sala.Id,
                    BeneficiarioId = participante.UsuarioId,
                    NumeroRonda = participante.NumeroTurno,
                    FechaVencimiento = fechaCalculada,
                    EstadoPeriodo = EstadoPeriodo.Pendiente // Nace con el estado Pendiente desde tu Enum
                };

                periodosNuevos.Add(nuevoPeriodo);

                // La fecha de vencimiento se calcula iterativamente sumando el intervalo de la Frecuencia a la FechaInicio de la sala[cite: 1].
                switch (sala.Frecuencia)
                {
                    case FrecuenciaSala.Semanal:
                        fechaCalculada = fechaCalculada.AddDays(7);
                        break;
                    case FrecuenciaSala.Quincenal:
                        fechaCalculada = fechaCalculada.AddDays(15);
                        break;
                    case FrecuenciaSala.Mensual:
                        fechaCalculada = fechaCalculada.AddMonths(1);
                        break;
                }
            }

            _context.Periodos.AddRange(periodosNuevos);

            // 6. Transición de Estado: Una vez generado el calendario, el estado de la sala cambia permanentemente a EnCurso[cite: 1].
            sala.Estado = EstadoSala.EnCurso;

            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "San iniciado exitosamente. Calendario de periodos generado." });
        }

        //Cancelar San
        [HttpPut("{salaId}/cancelar")]
        public async Task<IActionResult> CancelarSan(Guid salaId)
        {
            var usuarioLogueadoId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var sala = await _context.Salas.FirstOrDefaultAsync(s => s.Id == salaId);

            if (sala == null)
                return NotFound(new { Mensaje = "La sala no existe." });

            // 1. Aislamiento de Permisos: Solo el Organizador puede cancelar el San[cite: 2]
            if (sala.CreadorId.ToString() != usuarioLogueadoId)
                return StatusCode(403, new { Mensaje = "Solo el Organizador puede cancelar el San." });

            // 2. Evitar redundancia: Validamos que no esté ya cancelada o finalizada
            if (sala.Estado == EstadoSala.Cancelada || sala.Estado == EstadoSala.Finalizada)
                return BadRequest(new { Mensaje = "Este San ya se encuentra archivado." });

            // 3. Soft Delete: Muta el estado a Cancelada (4) preservando el historial
            sala.Estado = EstadoSala.Cancelada;

            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "El San ha sido cancelado y archivado exitosamente." });
        }
    }

}
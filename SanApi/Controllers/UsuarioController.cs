using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SanApi.Datos;
using SanApi.Dtos;
using SanApi.Modelos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public UsuarioController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: api/usuario/registro
        [HttpPost("registro")]
        public async Task<IActionResult> RegistrarUsuario([FromBody] UsuarioRegistroDto dto)
        {
            // 1. Validar si el correo ya está registrado en el sistema
            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == dto.Correo);
            if (usuarioExistente != null)
            {
                return BadRequest(new { Mensaje = "Este correo ya está registrado en la aplicación." });
            }

            // 2. Generar un código aleatorio de 6 dígitos para la verificación "Hard-Gate"
            var rnd = new Random();
            string codigoGenerado = rnd.Next(100000, 999999).ToString();

            // 3. Ensamblar la entidad Usuario con todos sus campos
            var nuevoUsuario = new Usuario
            {
                // Nota: Id, FechaRegistro, ScoreRiesgo y Rol(1) ya tienen valores 
                // por defecto en tu clase, así que no es obligatorio ponerlos aquí, 
                // pero mapeamos los que vienen del DTO y la seguridad.
                NombreCompleto = dto.NombreCompleto,
                Correo = dto.Correo,
                Telefono = dto.Telefono, // ¡Agregado!
                ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena),

                // Campos de control de verificación
                CorreoVerificado = false,
                CodigoVerificacion = codigoGenerado
            };

            // 4. Guardar en la base de datos
            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            // 5. Devolver respuesta (Simulando el envío de correo por ahora)
            return Ok(new
            {
                Mensaje = "Usuario registrado. Revisa tu correo electrónico para obtener el código de verificación.",
                UsuarioId = nuevoUsuario.Id,
                // TODO: Recuerda quitar esto cuando implementemos el envío de correos reales
                CodigoSecretoParaPruebas = codigoGenerado
            });
        }

        //Verificar correo
        [HttpPost("verificar-correo")]
        public async Task<IActionResult> VerificarCorreo([FromBody] VerificarCorreoDto dto)
        {
            // 1. Buscamos al usuario usando el correo que viene en el DTO
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == dto.Correo);

            if (usuario == null)
            {
                return NotFound(new { Mensaje = "Usuario no encontrado." });
            }

            // 2. Validación de seguridad: Comparamos el código que envió la app 
            // contra el que guardamos en la base de datos al momento del registro
            if (usuario.CodigoVerificacion != dto.Codigo)
            {
                return BadRequest(new { Mensaje = "El código de verificación es incorrecto." });
            }

            // 3. Si el código coincide, activamos la cuenta
            usuario.CorreoVerificado = true;

            // 4. Limpiamos el código para que no pueda ser reutilizado
            usuario.CodigoVerificacion = null;

            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "¡Cuenta verificada con éxito!" });
        }

        // GET: api/usuario/perfil
        [HttpGet("perfil")]
        [Authorize]
        public async Task<ActionResult<UsuarioRespuestaDto>> ObtenerPerfilDesdeToken()
        {
            // El "User" contiene los Claims del Token JWT interceptado por el servidor
            var usuarioIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(usuarioIdClaim)) return Unauthorized();

            var usuario = await _context.Usuarios.FindAsync(Guid.Parse(usuarioIdClaim));
            if (usuario == null) return NotFound();

            return Ok(new UsuarioRespuestaDto
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono
            });
        }

        // GET: api/usuario/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioRespuestaDto>> ObtenerPorId(Guid id)
        {
            // Buscamos el usuario en la BD
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            // MAPEO MANUAL: De Entidad a DTO (Salida)
            var respuestaDto = new UsuarioRespuestaDto
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono
            };

            return Ok(respuestaDto);
        }

        // POST: api/usuario/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(UsuarioLoginDto dto)
        {
            // 1. Buscar al usuario en la base de datos por su correo
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == dto.Correo);

            // Si el correo no existe, damos un mensaje genérico por seguridad
            if (usuario == null)
            {
                return BadRequest("Correo o contraseña incorrectos.");
            }

            // 2. MAGIA DE BCRYPT: Comparamos la contraseña plana con el hash guardado
            bool contrasenaValida = BCrypt.Net.BCrypt.Verify(dto.Contrasena, usuario.ContrasenaHash);

            if (!contrasenaValida)
            {
                return BadRequest("Correo o contraseña incorrectos.");
            }

            if (!usuario.CorreoVerificado)
            {
                return Unauthorized(new { Mensaje = "Por favor, verifica tu correo electrónico antes de iniciar sesión.", RequiereVerificacion = true
            });
                
            }

            // 3. GENERAR EL TOKEN JWT
            // Traemos la llave secreta desde el appsettings.json
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Los "Claims" son los datos del usuario que viajarán encriptados dentro del token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
                new Claim("rol", usuario.Rol.ToString()) // Fundamental para los permisos más adelante
            };

            // Ensamblamos el token con sus configuraciones y tiempo de vida (ej. 2 horas)
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials);

            // Lo convertimos a una cadena de texto para enviarlo
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Devolvemos el token al usuario
            return Ok(new
            {
                Mensaje = "¡Login exitoso!",
                Token = tokenString,
                UsuarioId = usuario.Id,
                Nombre = usuario.NombreCompleto
            });
        }

        [HttpPut("perfil")]
        [Authorize]
        public async Task<IActionResult> ActualizarPerfil([FromBody] UsuarioActualizarDto dto)
        {
            // 1. Extraemos el ID del usuario desde los Claims del Token JWT
            var usuarioIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(usuarioIdClaim))
            {
                return Unauthorized(new { Mensaje = "No se pudo identificar al usuario." });
            }

            // 2. Buscamos el usuario en la base de datos
            var usuario = await _context.Usuarios.FindAsync(Guid.Parse(usuarioIdClaim));

            if (usuario == null)
            {
                return NotFound(new { Mensaje = "Usuario no encontrado." });
            }

            // 3. Actualizamos los campos
            usuario.NombreCompleto = dto.NombreCompleto;
            usuario.Correo = dto.Correo;
            usuario.Telefono = dto.Telefono;

            // 4. Guardamos los cambios usando Entity Framework
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Perfil actualizado correctamente." });
        }
    }
}

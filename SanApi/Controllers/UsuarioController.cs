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

        // POST: api/usuario/registrar
        [HttpPost("registrar")]
        public async Task<ActionResult<UsuarioRespuestaDto>> Registrar(UsuarioRegistroDto dto)
        {
            // 1. Validar si el correo ya existe en la BD
            var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Correo == dto.Correo);
            if (existeUsuario)
            {
                return BadRequest("El correo ya está registrado.");
            }

            // 2. MAPEO MANUAL: De DTO (Entrada) a Entidad (Base de Datos)
            var nuevoUsuario = new Usuario
            {
                NombreCompleto = dto.NombreCompleto,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena),
                FechaRegistro = DateTime.Now,
                Rol = 2,    // Asignamos valores por defecto seguros
                ScoreRiesgo = 0
            };

            // 3. Guardar en SQL Server
            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            // 4. MAPEO MANUAL: De Entidad a DTO (Salida)
            var respuestaDto = new UsuarioRespuestaDto
            {
                Id = nuevoUsuario.Id,
                NombreCompleto = nuevoUsuario.NombreCompleto,
                Correo = nuevoUsuario.Correo,
                Telefono = nuevoUsuario.Telefono
            };

            // 5. Retornar un código 201 (Creado) junto con los datos seguros
            return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevoUsuario.Id }, respuestaDto);
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
    }
}

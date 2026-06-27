using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanApi.Datos;
using SanApi.Dtos;
using SanApi.Modelos;
using System.Security.Claims;

namespace SanApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ContactosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContactosController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: Traer los contactos del usuario logueado
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContactoDto>>> GetContactos()
        {
            // Extraemos el ID del usuario del token como Guid
            var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (usuarioIdString == null) return Unauthorized();
            var usuarioId = Guid.Parse(usuarioIdString);

            var contactos = await _context.Contactos
                .Where(c => c.UsuarioId == usuarioId)
                .Select(c => new ContactoDto
                {
                    Id = c.Id, // Ahora es Guid
                    Nombre = c.Nombre,
                    Telefono = c.Telefono,
                    Email = c.Email
                }).ToListAsync();

            return Ok(contactos);
        }

        // 2. POST: Crear un nuevo contacto
        [HttpPost]
        public async Task<ActionResult<ContactoDto>> PostContacto(ContactoDto dto)
        {
            var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (usuarioIdString == null) return Unauthorized();
            var usuarioId = Guid.Parse(usuarioIdString);

            var nuevoContacto = new Contacto
            {
                Id = Guid.NewGuid(), // Generamos el nuevo Guid para el contacto
                Nombre = dto.Nombre,
                Telefono = dto.Telefono,
                Email = dto.Email,
                UsuarioId = usuarioId
            };

            _context.Contactos.Add(nuevoContacto);
            await _context.SaveChangesAsync();

            // Retornamos el DTO con el nuevo ID generado
            dto.Id = nuevoContacto.Id;
            return CreatedAtAction(nameof(GetContactos), new { id = nuevoContacto.Id }, dto);
        }
    }
}
using System.ComponentModel.DataAnnotations.Schema;

namespace SanApi.Modelos
{
    public class Contacto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public Guid UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; } = null!;
    }
}

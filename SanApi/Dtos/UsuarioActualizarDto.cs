using System.ComponentModel.DataAnnotations;

namespace SanApi.Dtos
{
    public class UsuarioActualizarDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string NombreCompleto { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;
    }
}

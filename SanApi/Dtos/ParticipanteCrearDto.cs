using System.ComponentModel.DataAnnotations;

namespace SanApi.Dtos
{
    public class ParticipanteCrearDto
    {
        [Required(ErrorMessage = "El ID de la sala es obligatorio.")]
        public Guid SalaId { get; set; }

        [Required(ErrorMessage = "El ID del usuario es obligatorio.")]
        public Guid UsuarioId { get; set; }

        [Required(ErrorMessage = "Debe asignar un número de turno al participante.")]
        [Range(1, 100, ErrorMessage = "El número de turno debe ser mayor a 0.")]
        public int NumeroTurno { get; set; }
    }
}

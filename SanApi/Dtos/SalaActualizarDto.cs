using SanApi.Modelos;
using System.ComponentModel.DataAnnotations;

namespace SanApi.Dtos
{
    public class SalaActualizarDto
    {
        [Required(ErrorMessage = "El nombre de la sala es obligatorio.")]
        [MaxLength(100)]
        public string NombreSala { get; set; } = string.Empty;

        [Required]
        public decimal MontoCuota { get; set; }

        [Required]
        public int Frecuencia { get; set; }

        [Required]
        public int CantidadParticipantes { get; set; }

        public bool EsPublica { get; set; }

        [EnumDataType(typeof(EstadoSala), ErrorMessage = "El estado enviado no es válido. Solo se permite del 1 al 4.")]
        public EstadoSala Estado { get; set; }// Permitimos cambiar el estado
    }
}

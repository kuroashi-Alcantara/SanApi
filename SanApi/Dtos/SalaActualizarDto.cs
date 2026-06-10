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

        [Required(ErrorMessage = "La frecuencia es obligatoria.")]
        [EnumDataType(typeof(FrecuenciaSala), ErrorMessage = "La frecuencia enviada no es válida. Solo se permite 1 (Semanal), 2 (Quincenal) o 3 (Mensual).")]
        public FrecuenciaSala Frecuencia { get; set; }

        [Required]
        public int CantidadParticipantes { get; set; }

        public bool EsPublica { get; set; }
        public bool PermitirMultiplesTurnos { get; set; }

        [EnumDataType(typeof(EstadoSala), ErrorMessage = "El estado enviado no es válido. Solo se permite del 1 al 4.")]
        public EstadoSala Estado { get; set; }
    }
}

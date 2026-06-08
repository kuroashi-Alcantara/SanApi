using System.ComponentModel.DataAnnotations;

namespace SanApi.Dtos
{
    public class SalaCrearDto
    {
        [Required(ErrorMessage = "El nombre de la sala es obligatorio.")]
        [MaxLength(100)]
        public string NombreSala { get; set; } = string.Empty;

        [Required]
        public decimal MontoCuota { get; set; }

        [Required]
        public int Frecuencia { get; set; } // 1: Semanal, 2: Quincenal, 3: Mensual

        [Required]
        public int CantidadParticipantes { get; set; }

        public bool EsPublica { get; set; } = false;

        [Required]
        public DateTime FechaInicio { get; set; }
    }
}

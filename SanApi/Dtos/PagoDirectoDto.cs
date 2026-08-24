using System.ComponentModel.DataAnnotations;

namespace SanApi.Dtos
{
    public class PagoDirectoDto
    {
        [Required]
        public Guid PeriodoId { get; set; }

        [Required]
        public Guid UsuarioPagadorId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal Monto { get; set; }
    }
}

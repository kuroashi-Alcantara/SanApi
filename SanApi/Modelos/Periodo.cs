using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanApi.Modelos
{
    public class Periodo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SalaId { get; set; }

        public int NumeroRonda { get; set; }

        public DateTime FechaVencimiento { get; set; }

        public Guid BeneficiarioId { get; set; }

        public int EstadoPeriodo { get; set; } = 1;

        public DateTime? FechaDesembolso { get; set; }

        [ForeignKey("SalaId")]
        public virtual Sala Sala { get; set; } = null!;

        [ForeignKey("BeneficiarioId")]
        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Usuario Beneficiario { get; set; } = null!;
    }
}

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanApi.Modelos
{
    public class Transaccion
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PeriodoId { get; set; }

        [Required]
        public Guid UsuarioPagadorId { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [MaxLength(500)]
        public string UrlVoucher { get; set; } = string.Empty;

        public int EstadoPago { get; set; } = 1;

        [ForeignKey("PeriodoId")]
        public virtual Periodo Periodo { get; set; } = null!;

        [ForeignKey("UsuarioPagadorId")]
        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Usuario UsuarioPagador { get; set; } = null!;
    }
}

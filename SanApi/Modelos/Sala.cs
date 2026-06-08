using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanApi.Modelos
{
    public class Sala
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CreadorId { get; set; }

        [Required, MaxLength(100)]
        public string NombreSala { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal MontoCuota { get; set; }

        [Required]
        public int Frecuencia { get; set; } // 1: Semanal, 2: Quincenal, 3: Mensual

        [Required]
        public int CantidadParticipantes { get; set; }

        public bool EsPublica { get; set; } = false;

        public EstadoSala Estado { get; set; } = EstadoSala.Reclutamiento;

        public DateTime FechaInicio { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [ForeignKey("CreadorId")]
        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Usuario Creador { get; set; } = null!;
    }

    public enum EstadoSala
    {
        Reclutamiento = 1, // Sala abierta, esperando que la gente se una
        EnCurso = 2,       // El San ya comenzó, juego cerrado
        Finalizada = 3,    // Todos cobraron con éxito
        Cancelada = 4      // El San fue anulado por el administrador
    }
}

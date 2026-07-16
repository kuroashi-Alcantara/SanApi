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
        // 2. Cambiamos el tipo de int a FrecuenciaSala
        public FrecuenciaSala Frecuencia { get; set; } = FrecuenciaSala.Mensual;

        [Required]
        public int CantidadParticipantes { get; set; }

        public bool EsPublica { get; set; } = false;

        // ¿Puede una persona comprar más de un número/turno?
        public bool PermitirMultiplesTurnos { get; set; } = false;

        // ==========================================================
        // NUEVAS REGLAS DE NEGOCIO (CONFIGURACIÓN DEL ORGANIZADOR)
        // ==========================================================

        // Determina si el sistema hace una tómbola o si el organizador asigna a mano
        public bool SorteoTurnosAleatorio { get; set; } = true;

        // Determina si se puede desembolsar sin que todos hayan pagado
        public bool PermiteDesembolsoAnticipado { get; set; } = false;

        // ==========================================================

        public EstadoSala Estado { get; set; } = EstadoSala.Reclutamiento;

        public DateTime FechaInicio { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [ForeignKey("CreadorId")]
        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Usuario Creador { get; set; } = null!;

       
        public virtual ICollection<ParticipanteSala> ParticipantesSalas { get; set; } = new List<ParticipanteSala>();
    }

    public enum FrecuenciaSala
    {
        Semanal = 1,
        Quincenal = 2,
        Mensual = 3
    }

    public enum EstadoSala
    {
        Reclutamiento = 1, // Sala abierta, esperando que la gente se una
        EnCurso = 2,       // El San ya comenzó, juego cerrado
        Finalizada = 3,    // Todos cobraron con éxito
        Cancelada = 4      // El San fue anulado por el administrador
    }
}

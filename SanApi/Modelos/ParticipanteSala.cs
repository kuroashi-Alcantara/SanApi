using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanApi.Modelos
{
    public class ParticipanteSala
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SalaId { get; set; }

        [Required] // MANTENIDO
        public Guid UsuarioId { get; set; }

        public int NumeroTurno { get; set; }

        // CAMBIO: Ahora inicia por defecto en Pendiente (0)
        public EstadoParticipacion EstadoParticipacion { get; set; } = EstadoParticipacion.Pendiente;

        // NUEVO: Fecha en la que ingresó el código
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        [ForeignKey("SalaId")]
        public virtual Sala Sala { get; set; } = null!;

        [ForeignKey("UsuarioId")]
        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Usuario Usuario { get; set; } = null!;
    }

    public enum EstadoParticipacion
    {
        Pendiente = 0, // Cuando ingresa el código (Esperando aprobación)
        Activo = 1,    // Cuando el admin lo acepta
        Retirado = 2,
        Sancionado = 3,
        Rechazado = 4  // Cuando el admin le deniega la entrada
    }
}

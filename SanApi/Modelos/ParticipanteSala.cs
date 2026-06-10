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

        [Required]
        public Guid UsuarioId { get; set; }

        public int NumeroTurno { get; set; }

        public EstadoParticipacion EstadoParticipacion { get; set; } = EstadoParticipacion.Activo;

        [ForeignKey("SalaId")]
        public virtual Sala Sala { get; set; } = null!;

        [ForeignKey("UsuarioId")]
        
        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Usuario Usuario { get; set; } = null!;
    }

    public enum EstadoParticipacion
    {
        Activo = 1,
        Retirado = 2,
        Sancionado = 3
    }
}

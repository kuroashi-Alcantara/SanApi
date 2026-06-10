using SanApi.Modelos;

namespace SanApi.Dtos
{
    public class ParticipanteRespuestaDto
    {
        public Guid Id { get; set; }
        public Guid SalaId { get; set; }
        public Guid UsuarioId { get; set; }
        public int NumeroTurno { get; set; }
        public EstadoParticipacion EstadoParticipacion { get; set; }
    }
}

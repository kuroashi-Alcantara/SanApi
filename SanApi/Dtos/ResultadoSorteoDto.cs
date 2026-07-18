using SanApi.Modelos;

namespace SanApi.Dtos
{
    public class ResultadoSorteoDto
    {
        public string NombreParticipante { get; set; } = string.Empty;
        public int NumeroTurno { get; set; }
        public EstadoParticipacion EstadoParticipacion { get; set; }
    }
}

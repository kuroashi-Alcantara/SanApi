using SanApi.Modelos;

namespace SanApi.Dtos
{
    public class SalaRespuestaDto
    {
        public Guid Id { get; set; }
        public Guid CreadorId { get; set; } // Aquí sí lo devolvemos para que el frontend sepa de quién es
        public string NombreSala { get; set; } = string.Empty;
        public decimal MontoCuota { get; set; }
        public FrecuenciaSala Frecuencia { get; set; }
        public int CantidadParticipantes { get; set; }
        public bool EsPublica { get; set; }
        public bool PermitirMultiplesTurnos { get; set; }
        public bool PermiteDesembolsoAnticipado { get; set; }
        public EstadoSala Estado { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaCreacion { get; set; }

        public List<ParticipanteSalaDto> Participantes { get; set; } = new List<ParticipanteSalaDto>();
    }


  }

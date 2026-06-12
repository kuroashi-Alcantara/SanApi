using SanApi.Modelos;

namespace SanApi.Dtos
{
    public class PeriodoRespuestaDto
    {
        public Guid Id { get; set; }
        public Guid SalaId { get; set; }
        public int NumeroRonda { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public Guid BeneficiarioId { get; set; }
        public EstadoPeriodo EstadoPeriodo { get; set; }
        public DateTime? FechaDesembolso { get; set; }
    }
}

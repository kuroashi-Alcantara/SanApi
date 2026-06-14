namespace SanApi.Dtos
{
    // 1. El DTO Principal que agrupa todo
    public class DashboardRespuestaDto
    {
        public List<SalaResumenDto> MisSalas { get; set; } = new List<SalaResumenDto>();
        public List<PagoPendienteDto> PagosPendientes { get; set; } = new List<PagoPendienteDto>();
        public List<CobroProximoDto> ProximosCobros { get; set; } = new List<CobroProximoDto>();
    }

    // 2. Resumen de las salas donde está inscrito
    public class SalaResumenDto
    {
        public Guid SalaId { get; set; }
        public string NombreSala { get; set; } = string.Empty;
        public decimal MontoCuota { get; set; }
        public string Frecuencia { get; set; } = string.Empty;
        public int MiTurno { get; set; }
    }

    // 3. Lo que el usuario debe pagar pronto
    public class PagoPendienteDto
    {
        public Guid PeriodoId { get; set; }
        public string NombreSala { get; set; } = string.Empty;
        public int NumeroRonda { get; set; }
        public decimal MontoAPagar { get; set; }
        public DateTime FechaVencimiento { get; set; }
    }

    // 4. Cuándo le toca recibir su dinero
    public class CobroProximoDto
    {
        public Guid PeriodoId { get; set; }
        public string NombreSala { get; set; } = string.Empty;
        public int NumeroRonda { get; set; }
        public decimal MontoEstimado { get; set; }
        public DateTime FechaCobro { get; set; }
    }
}

namespace SanApi.Dtos
{
    // El DTO Principal para la vista de Administrador
    public class AdminDashboardRespuestaDto
    {
        public List<SalaAdministradaDto> MisSalasOrganizadas { get; set; } = new List<SalaAdministradaDto>();
        public List<VoucherPendienteDto> VouchersPorRevisar { get; set; } = new List<VoucherPendienteDto>();
        public List<DesembolsoPendienteDto> DesembolsosPorHacer { get; set; } = new List<DesembolsoPendienteDto>();
    }

    // Resumen de las salas que él controla
    public class SalaAdministradaDto
    {
        public Guid SalaId { get; set; }
        public string NombreSala { get; set; } = string.Empty;
        public int ParticipantesActivos { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    // Alerta de transacciones que participantes subieron y el administrador debe evaluar
    public class VoucherPendienteDto
    {
        public Guid TransaccionId { get; set; }
        public string NombreSala { get; set; } = string.Empty;
        public int NumeroRonda { get; set; }
        public string NombrePagador { get; set; } = string.Empty; // Útil para saber a quién le aprueba
        public decimal Monto { get; set; }
        public string UrlVoucher { get; set; } = string.Empty;
    }

    // Rondas donde todos ya pagaron y el administrador debe transferir al beneficiario
    public class DesembolsoPendienteDto
    {
        public Guid PeriodoId { get; set; }
        public string NombreSala { get; set; } = string.Empty;
        public int NumeroRonda { get; set; }
        public string NombreBeneficiario { get; set; } = string.Empty; // A quién debe transferirle el organizador
        public decimal MontoTotalPozo { get; set; }
    }
}

using SanApi.Modelos;

namespace SanApi.Dtos
{
    public class TransaccionRespuestaDto
    {
        public Guid Id { get; set; }
        public Guid PeriodoId { get; set; }
        public Guid UsuarioPagadorId { get; set; }
        public decimal Monto { get; set; }
        public string UrlVoucher { get; set; } = string.Empty;
        public EstadoPago EstadoPago { get; set; }
    }
}

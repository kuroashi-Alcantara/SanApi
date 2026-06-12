using System.ComponentModel.DataAnnotations;

namespace SanApi.Dtos
{
    public class TransaccionCrearDto
    {
        [Required(ErrorMessage = "El ID del período es obligatorio.")]
        public Guid PeriodoId { get; set; }

        [Required(ErrorMessage = "El ID del usuario pagador es obligatorio.")]
        public Guid UsuarioPagadorId { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto del pago debe ser mayor a 0.")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "Debe enviar el comprobante o voucher de pago.")]
        [MaxLength(500, ErrorMessage = "La ruta del voucher es demasiado larga.")]
        public string UrlVoucher { get; set; } = string.Empty;
    }
}

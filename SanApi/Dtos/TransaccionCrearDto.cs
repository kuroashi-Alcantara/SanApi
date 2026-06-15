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

        // Cambiamos el string por IFormFile para recibir los bytes físicos de la foto
        [Required(ErrorMessage = "Debe adjuntar la imagen del comprobante.")]
        public IFormFile ImagenVoucher { get; set; } = null!;
    }
}

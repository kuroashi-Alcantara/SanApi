using SanApi.Modelos;
using System.ComponentModel.DataAnnotations;

namespace SanApi.Dtos
{
    public class EvaluarTransaccionDto
    {
        [Required]
        public EstadoPago Estado { get; set; } // Recibirá Aprobado (2) o Rechazado (3)
    }
}

using System.ComponentModel.DataAnnotations;

namespace SanApi.Dtos
{
    public class DesembolsarPeriodoDto
    {
        [Required(ErrorMessage = "Debe proporcionar la URL o ruta del comprobante de desembolso.")]
        public string UrlComprobante { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace SanApi.Dtos
{
    public class CambiarContrasenaDto
    {
       
        public string ContrasenaActual { get; set; }
        public string NuevaContrasena { get; set; }
    }
}

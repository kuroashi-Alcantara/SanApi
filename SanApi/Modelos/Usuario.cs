using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanApi.Modelos
{
    public class Usuario
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Correo { get; set; } = string.Empty;

        // verificar correo
        public bool CorreoVerificado { get; set; } = false; // Empieza en falso
        public string? CodigoVerificacion { get; set; } // El código de 4 o 6 dígitos (puede ser nulo)

        [Required, MaxLength(20)]
        public string Telefono { get; set; } = string.Empty;

        [Required]
        public string ContrasenaHash { get; set; } = string.Empty;

        [Required]
        public int Rol { get; set; } = 1;

        public int ScoreRiesgo { get; set; } = 0;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string NivelReputacion => ScoreRiesgo <= 100 ? "Inicial" :
                                         ScoreRiesgo <= 300 ? "Bronce" :
                                         ScoreRiesgo <= 700 ? "Plata" : "Oro";
    }
}

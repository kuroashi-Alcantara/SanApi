using System.Text.Json.Serialization;

namespace SanApi.Dtos
{
    public class AsignarTurnosDto
    {
        [JsonPropertyName("turnos")]
        public List<TurnoItemDto> Turnos { get; set; } = new();
    }

    public class TurnoItemDto
    {
        [JsonPropertyName("usuarioId")]
        public Guid UsuarioId { get; set; }

        [JsonPropertyName("numeroTurno")]
        public int NumeroTurno { get; set; }
    }
}

namespace SanApi.Dtos
{
         // DTO pequeñito para representar a la persona dentro de la sala
        public class ParticipanteSalaDto
        {
            public Guid UsuarioId { get; set; }
            public string Nombre { get; set; } // O NombreCompleto, según tu entidad Usuario
            public int? NumeroTurno { get; set; }
        // Aquí a futuro podemos agregar EstadoDePago, etc.
        public int EstadoParticipacion { get; set; } = 0;
    }
    
}

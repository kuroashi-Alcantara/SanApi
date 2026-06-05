using Microsoft.EntityFrameworkCore;
using SanApi.Modelos;

namespace SanApi.Datos
{
    public class AppDbContext : DbContext
    {
        // El constructor recibe las opciones de configuración (cadena de conexión, provider, etc.)
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Definición de las tablas (DbSets)
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Sala> Salas { get; set; }
        public DbSet<ParticipanteSala> ParticipantesSala { get; set; }
        public DbSet<Periodo> Periodos { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ejemplo de configuración: Asegurar que el correo sea único
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Correo)
                .IsUnique();
        }
    }
}

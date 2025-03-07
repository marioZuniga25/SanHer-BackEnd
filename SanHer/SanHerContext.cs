using Microsoft.EntityFrameworkCore;
using SanHer.Models;

namespace SanHer
{
    public class SanHerContext: DbContext
    {
        public SanHerContext(DbContextOptions<SanHerContext> options) : base(options)
        { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<DiaNoLaborable> DiasNoLaborables { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Usuario>(usuario =>
            {
                usuario.ToTable("Usuario");
                usuario.HasKey(u => u.Id);
                usuario.Property(u => u.Id).ValueGeneratedOnAdd().UseIdentityColumn();
                usuario.Property(u => u.Nombre).IsRequired();
                usuario.Property(u => u.Apellido1).IsRequired();
                usuario.Property(u => u.Apellido2).IsRequired();
                usuario.Property(u => u.Correo).IsRequired();
                usuario.Property(u => u.Contrasenia).IsRequired();
                usuario.Property(u => u.Rol).IsRequired();
                usuario.Property(u => u.FechaRegistro).IsRequired();
                usuario.Property(u => u.UltimaConexion).IsRequired();

            });

            modelBuilder.Entity<Cita>(cita =>
            {
                cita.ToTable("Cita");
                cita.HasKey(c => c.Id);
                cita.Property(c => c.Id).ValueGeneratedOnAdd().UseIdentityColumn();
                cita.Property(c => c.Fecha).IsRequired();
                cita.Property(c => c.telefono).IsRequired();
                cita.Property(c => c.IdUsuario).IsRequired();
                cita.Property(c => c.IdContadorAsignado).IsRequired();
                cita.Property(c => c.Estatus).IsRequired();
                cita.Property(c => c.Horario).IsRequired();

            });


            modelBuilder.Entity<Horario>(horario =>
            {
                horario.ToTable("Cita");
                horario.HasKey(c => c.Id);
                horario.Property(c => c.Id).ValueGeneratedOnAdd().UseIdentityColumn();
                horario.Property(c => c.IdContador).IsRequired();
                horario.Property(c => c.DiaSemana).IsRequired();
                horario.Property(c => c.Disponibilidad).IsRequired();
                horario.Property(c => c.HoraInicio).IsRequired();
                horario.Property(c => c.HoraFin).IsRequired();

            });

            modelBuilder.Entity<DiaNoLaborable>(dnl=>
            {
                dnl.ToTable("Cita");
                dnl.HasKey(c => c.Id);
                dnl.Property(c => c.Id).ValueGeneratedOnAdd().UseIdentityColumn();
                dnl.Property(c => c.Fecha).IsRequired();
                dnl.Property(c => c.Descripcion).IsRequired();
            });
        }
    }
}

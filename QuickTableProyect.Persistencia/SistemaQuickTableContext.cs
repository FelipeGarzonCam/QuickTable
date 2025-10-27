using QuickTableProyect.Dominio;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace QuickTableProyect.Persistencia.Datos
{
    public class SistemaQuickTableContext : DbContext
    {
        static string connectionString =
            "Server=localhost;" +
            "Database=QuickTableProyectDB;" +
            "Trusted_Connection=True;";

        public SistemaQuickTableContext() : base(connectionString)
        {
            this.Database.CommandTimeout = 10;
        }

        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<PedidosActivos> PedidosActivos { get; set; }
        public DbSet<ItemDetalle> ItemDetalles { get; set; }

        public DbSet<HistorialPedido> HistorialPedidos { get; set; }
        public DbSet<HistorialDetalle> HistorialDetalles { get; set; }

        public DbSet<RegistroSesion> RegistroSesiones { get; set; } 

        public DbSet<TarjetaRC> TarjetasRC { get; set; }
        public DbSet<Codigo2FA> Codigos2FA { get; set; }
        public DbSet<HistorialBackup> HistorialBackups { get; set; }



        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configuración para TarjetaUID en Empleado
            modelBuilder.Entity<Empleado>()
                .Property(e => e.TarjetaUID)
                .HasMaxLength(200)
                .IsOptional();

            // Configuración para MarcoTarjetaSalida en RegistroSesion
            modelBuilder.Entity<RegistroSesion>()
                .Property(r => r.MarcoTarjetaSalida)
                .IsRequired()
                .HasColumnType("bit");

            // Configuración para EsParaTarjetaEmpleado en Codigo2FA
            modelBuilder.Entity<Codigo2FA>()
                .Property(c => c.EsParaTarjetaEmpleado)
                .IsRequired()
                .HasColumnType("bit");

            // Eliminar pluralización automática de nombres de tablas
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);

            // Configuración para HistorialBackup
            modelBuilder.Entity<HistorialBackup>()
                .Property(h => h.NombreArchivo)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<HistorialBackup>()
                .Property(h => h.RutaArchivo)
                .HasMaxLength(500)
                .IsRequired();

            modelBuilder.Entity<HistorialBackup>()
                .Property(h => h.TipoBackup)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<HistorialBackup>()
                .Property(h => h.Estado)
                .HasMaxLength(50)
                .IsRequired();
        }
    }
} 



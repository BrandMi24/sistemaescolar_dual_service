using ControlEscolar.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlEscolar.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets Preinscripcion
        public DbSet<PreinscripcionEntity> Preinscripciones { get; set; }
        public DbSet<PreinscripcionDatosPersonalesEntity> PreinscripcionDatosPersonales { get; set; }
        public DbSet<PreinscripcionDomicilioEntity> PreinscripcionDomicilio { get; set; }
        public DbSet<PreinscripcionTutorEntity> PreinscripcionTutor { get; set; }
        public DbSet<PreinscripcionEscolarEntity> PreinscripcionEscolar { get; set; }
        public DbSet<PreinscripcionSaludEntity> PreinscripcionSalud { get; set; }

        // DbSet Inscripcion
        public DbSet<InscripcionEntity> Inscripciones { get; set; }

        // ==========================================
        // NUEVO: DbSets Trámites Escolares
        // 
        // ==========================================
        // NUEVO: DbSets Trámites Escolares (Clases Reales)
        // ==========================================
        public DbSet<Requisito_Tramite> TramitesRequisitos { get; set; }
        public DbSet<Solicitud> TramitesSolicitudes { get; set; }
        public DbSet<DetalleDocumentos> TramitesDetalleDocumentos { get; set; }
        public DbSet<Cat_Tramites> CategoriasTramites { get; set; }
        public DbSet<ManagementUser> ManagementUsers { get; set; }
        public DbSet<ConfiguracionFichasEntity> ConfiguracionFichas { get; set; }
        public DbSet<PeriodoInscripcionEntity> PeriodosInscripcion { get; set; }

        // ==========================================
        // NUEVO: Módulo de Salud (Enfermería y Psicología)
        // ==========================================
        public DbSet<VisitaMedica> Visitas { get; set; }
        public DbSet<VisitaPsicologica> VisitasPsicologicas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<PreinscripcionEntity>(entity =>
            {
                entity.ToTable("Preinscripciones");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Folio).IsUnique().HasFilter("[Folio] IS NOT NULL");
                entity.Property(e => e.Folio).HasMaxLength(20);
                entity.Property(e => e.CarreraSolicitada).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Promedio).HasColumnType("DECIMAL(4,2)");
                entity.Property(e => e.MedioDifusion).HasMaxLength(150);
                entity.Property(e => e.EstadoPreinscripcion).HasMaxLength(50).HasDefaultValue("Pendiente");
                entity.Property(e => e.FechaPreinscripcion).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<PreinscripcionDatosPersonalesEntity>(entity =>
            {
                entity.ToTable("PreinscripcionDatosPersonales");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApellidoPaterno).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApellidoMaterno).HasMaxLength(100);
                entity.Property(e => e.CURP).IsRequired().HasMaxLength(18).IsFixedLength();
                entity.Property(e => e.Sexo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.EstadoCivil).HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Telefono).HasMaxLength(20);

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.DatosPersonales)
                      .HasForeignKey<PreinscripcionDatosPersonalesEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PreinscripcionDomicilioEntity>(entity =>
            {
                entity.ToTable("PreinscripcionDomicilio");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Municipio).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CodigoPostal).HasMaxLength(5).IsFixedLength();
                entity.Property(e => e.Colonia).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Calle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.NumeroExterior).IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.Domicilio)
                      .HasForeignKey<PreinscripcionDomicilioEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PreinscripcionTutorEntity>(entity =>
            {
                entity.ToTable("PreinscripcionTutor");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TutorNombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Parentesco).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Telefono).IsRequired().HasMaxLength(20);

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.Tutor)
                      .HasForeignKey<PreinscripcionTutorEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PreinscripcionEscolarEntity>(entity =>
            {
                entity.ToTable("PreinscripcionEscolar");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EscuelaProcedencia).IsRequired().HasMaxLength(200);
                entity.Property(e => e.EstadoEscuela).HasMaxLength(100);
                entity.Property(e => e.MunicipioEscuela).HasMaxLength(100);
                entity.Property(e => e.CCT).HasMaxLength(20);

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.DatosEscolares)
                      .HasForeignKey<PreinscripcionEscolarEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PreinscripcionSaludEntity>(entity =>
            {
                entity.ToTable("PreinscripcionSalud");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ServicioMedico).HasMaxLength(100);
                entity.Property(e => e.DiscapacidadDescripcion).HasMaxLength(250);
                entity.Property(e => e.ComunidadIndigenaDescripcion).HasMaxLength(150);
                entity.Property(e => e.Comentarios).HasMaxLength(500);

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.Salud)
                      .HasForeignKey<PreinscripcionSaludEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InscripcionEntity>(entity =>
            {
                entity.ToTable("Inscripciones");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Matricula).IsUnique();
                entity.Property(e => e.CarreraSolicitada).IsRequired().HasMaxLength(200);
                entity.Property(e => e.MatriculaTSU).HasMaxLength(20);
                entity.Property(e => e.Matricula).HasMaxLength(20);
                entity.Property(e => e.ActaNacimientoPath).HasMaxLength(500);
                entity.Property(e => e.CurpPdfPath).HasMaxLength(500);
                entity.Property(e => e.BoletaPdfPath).HasMaxLength(500);
                entity.Property(e => e.EstadoInscripcion).HasMaxLength(50).HasDefaultValue("Pendiente");
                entity.Property(e => e.FechaInscripcion).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Preinscripcion)
                      .WithMany()
                      .HasForeignKey(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // NUEVO: MÓDULO DE TRÁMITES (STORED PROCEDURES)
            // ==========================================
            // Le indicamos a Entity Framework que estas clases no son tablas físicas,
            // sino ViewModels que reciben datos crudos de los Stored Procedures.
            modelBuilder.Entity<DetalleSolicitudViewModel>().HasNoKey();
            modelBuilder.Entity<InfoAlumnoViewModel>().HasNoKey();
            modelBuilder.Entity<TramiteResult>().HasNoKey();
            modelBuilder.Entity<ArchivoDescargaViewModel>().HasNoKey();
            modelBuilder.Entity<RequisitoRevisionViewModel>().HasNoKey();
            modelBuilder.Entity<RequisitoSolicitudViewModel>().HasNoKey();
            modelBuilder.Entity<ConfiguracionFichasEntity>(entity =>
            {
                entity.ToTable("ConfiguracionFichas");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Carrera).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<PeriodoInscripcionEntity>(entity =>
            {
                entity.ToTable("PeriodoInscripcion");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // ==========================================
            // NUEVO: Configuración de Tablas de Salud
            // ==========================================
            modelBuilder.Entity<VisitaMedica>(entity =>
            {
                entity.ToTable("Visitas"); // Nombre físico de la tabla en SQL
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Matricula).IsRequired().HasMaxLength(20);
                entity.Property(e => e.FechaVisita).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Diagnostico).IsRequired();
            });

            modelBuilder.Entity<VisitaPsicologica>(entity =>
            {
                entity.ToTable("VisitasPsicologicas"); // Nombre físico de la tabla en SQL
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Matricula).IsRequired().HasMaxLength(20);
                entity.Property(e => e.FechaVisita).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.MotivoConsulta).IsRequired();
            });
        }
    }
}
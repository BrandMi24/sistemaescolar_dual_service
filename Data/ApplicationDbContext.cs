using ControlEscolar.Models;
using ControlEscolar.Models.ManagementOperational;
using ControlEscolar.Models.Operational;
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

        // DbSets operativos para Modelo Dual y Servicio Social
        public DbSet<Person> Persons { get; set; }
        public DbSet<Student> StudentsOperational { get; set; }
        public DbSet<Teacher> TeachersOperational { get; set; }
        public DbSet<Career> CareersOperational { get; set; }
        public DbSet<Group> GroupsOperational { get; set; }
        public DbSet<OperationalProgram> OperationalPrograms { get; set; }
        public DbSet<OperationalOrganization> OperationalOrganizations { get; set; }
        public DbSet<OperationalStudentAssignment> OperationalStudentAssignments { get; set; }
        public DbSet<OperationalDocument> OperationalDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<PreinscripcionEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_table");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("academiccontrol_preinscription_ID");
                entity.Property(e => e.Folio).HasColumnName("academiccontrol_preinscription_folio").HasMaxLength(20);
                entity.Property(e => e.CarreraSolicitada).HasColumnName("academiccontrol_preinscription_careerRequested").IsRequired().HasMaxLength(200);
                entity.Property(e => e.Promedio).HasColumnName("academiccontrol_preinscription_average").HasColumnType("DECIMAL(4,2)");
                entity.Property(e => e.MedioDifusion).HasColumnName("academiccontrol_preinscription_diffusionMedia").HasMaxLength(150);
                entity.Property(e => e.EstadoPreinscripcion).HasColumnName("academiccontrol_preinscription_state").HasMaxLength(50).HasDefaultValue("Pendiente");
                entity.Property(e => e.FechaPreinscripcion).HasColumnName("academiccontrol_preinscription_registrationDate");
            });

            modelBuilder.Entity<PreinscripcionDatosPersonalesEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_personaldata_table");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("academiccontrol_preinscription_personaldata_ID");
                entity.Property(e => e.PreinscripcionId).HasColumnName("academiccontrol_preinscription_personaldata_preinscriptionID");
                entity.Property(e => e.Nombre).HasColumnName("academiccontrol_preinscription_personaldata_name").IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApellidoPaterno).HasColumnName("academiccontrol_preinscription_personaldata_paternalSurname").IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApellidoMaterno).HasColumnName("academiccontrol_preinscription_personaldata_maternalSurname").HasMaxLength(100);
                entity.Property(e => e.CURP).HasColumnName("academiccontrol_preinscription_personaldata_CURP").IsRequired().HasMaxLength(18).IsFixedLength();
                entity.Property(e => e.FechaNacimiento).HasColumnName("academiccontrol_preinscription_personaldata_birthDate");
                entity.Property(e => e.Sexo).HasColumnName("academiccontrol_preinscription_personaldata_gender").IsRequired().HasMaxLength(20);
                entity.Property(e => e.EstadoCivil).HasColumnName("academiccontrol_preinscription_personaldata_maritalStatus").HasMaxLength(50);
                entity.Property(e => e.Email).HasColumnName("academiccontrol_preinscription_personaldata_email").IsRequired().HasMaxLength(200);
                entity.Property(e => e.Telefono).HasColumnName("academiccontrol_preinscription_personaldata_phone").HasMaxLength(20);

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.DatosPersonales)
                      .HasForeignKey<PreinscripcionDatosPersonalesEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PreinscripcionDomicilioEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_address_table");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("academiccontrol_preinscription_address_ID");
                entity.Property(e => e.PreinscripcionId).HasColumnName("academiccontrol_preinscription_address_preinscriptionID");
                entity.Property(e => e.Estado).HasColumnName("academiccontrol_preinscription_address_state").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Municipio).HasColumnName("academiccontrol_preinscription_address_municipality").IsRequired().HasMaxLength(100);
                entity.Property(e => e.CodigoPostal).HasColumnName("academiccontrol_preinscription_address_zipCode").HasMaxLength(5).IsFixedLength();
                entity.Property(e => e.Colonia).HasColumnName("academiccontrol_preinscription_address_neighborhood").IsRequired().HasMaxLength(150);
                entity.Property(e => e.Calle).HasColumnName("academiccontrol_preinscription_address_street").IsRequired().HasMaxLength(200);
                entity.Property(e => e.NumeroExterior).HasColumnName("academiccontrol_preinscription_address_exteriorNumber").IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.Domicilio)
                      .HasForeignKey<PreinscripcionDomicilioEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PreinscripcionTutorEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_tutor_table");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("academiccontrol_preinscription_tutor_ID");
                entity.Property(e => e.PreinscripcionId).HasColumnName("academiccontrol_preinscription_tutor_preinscriptionID");
                entity.Property(e => e.TutorNombre).HasColumnName("academiccontrol_preinscription_tutor_fullName").IsRequired().HasMaxLength(200);
                entity.Property(e => e.Parentesco).HasColumnName("academiccontrol_preinscription_tutor_relationship").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Telefono).HasColumnName("academiccontrol_preinscription_tutor_phone").IsRequired().HasMaxLength(20);

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.Tutor)
                      .HasForeignKey<PreinscripcionTutorEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PreinscripcionEscolarEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_academic_table");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("academiccontrol_preinscription_academic_ID");
                entity.Property(e => e.PreinscripcionId).HasColumnName("academiccontrol_preinscription_academic_preinscriptionID");
                entity.Property(e => e.EscuelaProcedencia).HasColumnName("academiccontrol_preinscription_academic_originSchool").IsRequired().HasMaxLength(200);
                entity.Property(e => e.EstadoEscuela).HasColumnName("academiccontrol_preinscription_academic_schoolState").HasMaxLength(100);
                entity.Property(e => e.MunicipioEscuela).HasColumnName("academiccontrol_preinscription_academic_schoolMunicipality").HasMaxLength(100);
                entity.Property(e => e.CCT).HasColumnName("academiccontrol_preinscription_academic_CCT").HasMaxLength(20);
                entity.Property(e => e.InicioBachillerato).HasColumnName("academiccontrol_preinscription_academic_startDate");
                entity.Property(e => e.FinBachillerato).HasColumnName("academiccontrol_preinscription_academic_endDate");

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.DatosEscolares)
                      .HasForeignKey<PreinscripcionEscolarEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PreinscripcionSaludEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_health_table");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("academiccontrol_preinscription_health_ID");
                entity.Property(e => e.PreinscripcionId).HasColumnName("academiccontrol_preinscription_health_preinscriptionID");
                entity.Property(e => e.ServicioMedico).HasColumnName("academiccontrol_preinscription_health_medicalService").HasMaxLength(100);
                entity.Property(e => e.TieneDiscapacidad).HasColumnName("academiccontrol_preinscription_health_hasDisability");
                entity.Property(e => e.DiscapacidadDescripcion).HasColumnName("academiccontrol_preinscription_health_disabilityDescription").HasMaxLength(250);
                entity.Property(e => e.ComunidadIndigena).HasColumnName("academiccontrol_preinscription_health_indigenousCommunity");
                entity.Property(e => e.ComunidadIndigenaDescripcion).HasColumnName("academiccontrol_preinscription_health_indigenousCommunityDescription").HasMaxLength(150);
                entity.Property(e => e.Comentarios).HasColumnName("academiccontrol_preinscription_health_comments").HasMaxLength(500);

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.Salud)
                      .HasForeignKey<PreinscripcionSaludEntity>(e => e.PreinscripcionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InscripcionEntity>(entity =>
            {
                entity.ToTable("academiccontrol_inscription_table");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("academiccontrol_inscription_ID");
                entity.Property(e => e.PreinscripcionId).HasColumnName("academiccontrol_inscription_preinscriptionID");
                entity.HasIndex(e => e.Matricula).IsUnique();
                entity.Property(e => e.CarreraSolicitada).HasColumnName("academiccontrol_inscription_careerRequested").IsRequired().HasMaxLength(200);
                entity.Property(e => e.TieneMatriculaTSU).HasColumnName("academiccontrol_inscription_hasTSUEnrollment");
                entity.Property(e => e.MatriculaTSU).HasColumnName("academiccontrol_inscription_TSUEnrollment").HasMaxLength(20);
                entity.Property(e => e.Matricula).HasColumnName("academiccontrol_inscription_enrollment").HasMaxLength(20);
                entity.Property(e => e.ActaNacimientoPath).HasColumnName("academiccontrol_inscription_birthCertificatePath").HasMaxLength(500);
                entity.Property(e => e.CurpPdfPath).HasColumnName("academiccontrol_inscription_curpPdfPath").HasMaxLength(500);
                entity.Property(e => e.BoletaPdfPath).HasColumnName("academiccontrol_inscription_transcriptPath").HasMaxLength(500);
                entity.Property(e => e.EstadoInscripcion).HasColumnName("academiccontrol_inscription_state").HasMaxLength(50).HasDefaultValue("Pendiente");
                entity.Property(e => e.FechaInscripcion).HasColumnName("academiccontrol_inscription_registrationDate");

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
                entity.ToTable("academiccontrol_inscription_ticketconfig_table");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("academiccontrol_inscription_ticketconfig_ID");
                entity.Property(e => e.Carrera).HasColumnName("academiccontrol_inscription_ticketconfig_career").IsRequired().HasMaxLength(200);
                entity.Property(e => e.LimiteFichas).HasColumnName("academiccontrol_inscription_ticketconfig_limit");
                entity.Property(e => e.FechaInicio).HasColumnName("academiccontrol_inscription_ticketconfig_startDate");
                entity.Property(e => e.FechaFin).HasColumnName("academiccontrol_inscription_ticketconfig_endDate");
                entity.Property(e => e.Activo).HasColumnName("academiccontrol_inscription_ticketconfig_status");
                entity.Property(e => e.FechaCreacion).HasColumnName("academiccontrol_inscription_ticketconfig_createdDate");
                entity.Property(e => e.FechaActualizacion).HasColumnName("academiccontrol_inscription_ticketconfig_updatedDate");
            });

            modelBuilder.Entity<PeriodoInscripcionEntity>(entity =>
            {
                entity.ToTable("academiccontrol_inscription_period_table");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("academiccontrol_inscription_period_ID");
                entity.Property(e => e.Nombre).HasColumnName("academiccontrol_inscription_period_name").IsRequired().HasMaxLength(100);
                entity.Property(e => e.FechaInicio).HasColumnName("academiccontrol_inscription_period_startDate");
                entity.Property(e => e.FechaFin).HasColumnName("academiccontrol_inscription_period_endDate");
                entity.Property(e => e.Activo).HasColumnName("academiccontrol_inscription_period_status");
                entity.Property(e => e.FechaCreacion).HasColumnName("academiccontrol_inscription_period_createdDate");
            });

            // ==========================================
            // MÓDULOS OPERATIVOS (DUAL / SERVICIO SOCIAL)
            // ==========================================
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Person)
                .WithMany()
                .HasForeignKey(s => s.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Career)
                .WithMany()
                .HasForeignKey(s => s.CareerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Group)
                .WithMany()
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Person)
                .WithMany()
                .HasForeignKey(t => t.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OperationalProgram>()
                .HasOne(p => p.Career)
                .WithMany()
                .HasForeignKey(p => p.CareerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OperationalProgram>()
                .HasOne(p => p.Coordinator)
                .WithMany()
                .HasForeignKey(p => p.CoordinatorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OperationalStudentAssignment>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OperationalStudentAssignment>()
                .HasOne(a => a.Program)
                .WithMany(p => p.StudentAssignments)
                .HasForeignKey(a => a.ProgramId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OperationalStudentAssignment>()
                .HasOne(a => a.Organization)
                .WithMany(o => o.StudentAssignments)
                .HasForeignKey(a => a.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OperationalStudentAssignment>()
                .HasOne(a => a.Teacher)
                .WithMany()
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OperationalDocument>()
                .HasOne(d => d.Assignment)
                .WithMany(a => a.Documents)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OperationalDocument>()
                .HasOne(d => d.ReviewedByTeacher)
                .WithMany()
                .HasForeignKey(d => d.ReviewedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
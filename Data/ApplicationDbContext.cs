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
        public DbSet<CuatrimestreCatalog> CuatrimestresCatalog { get; set; }
        public DbSet<OperationalProgram> OperationalPrograms { get; set; }
        public DbSet<OperationalOrganization> OperationalOrganizations { get; set; }
        public DbSet<OperationalStudentAssignment> OperationalStudentAssignments { get; set; }
        public DbSet<OperationalDocument> OperationalDocuments { get; set; }
        public DbSet<OperationalModuleFlowConfig> OperationalModuleFlowConfigs { get; set; }
        public DbSet<OperationalModuleStepRule> OperationalModuleStepRules { get; set; }
        // ==========================================
        // NUEVO: Módulo de Salud (Enfermería y Psicología)
        // ==========================================
        public DbSet<VisitaMedica> Visitas { get; set; }
        public DbSet<VisitaPsicologica> VisitasPsicologicas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  PREINSCRIPCION 
            modelBuilder.Entity<PreinscripcionEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_table");
                entity.HasKey(e => e.academiccontrol_preinscription_ID);
                entity.Property(e => e.academiccontrol_preinscription_ID)
                      .HasColumnName("academiccontrol_preinscription_ID");
                entity.Property(e => e.academiccontrol_preinscription_folio)
                      .HasColumnName("academiccontrol_preinscription_folio")
                      .HasMaxLength(20);
                entity.Property(e => e.academiccontrol_preinscription_careerRequested)
                      .HasColumnName("academiccontrol_preinscription_careerRequested")
                      .IsRequired().HasMaxLength(200);
                entity.Property(e => e.academiccontrol_preinscription_average)
                      .HasColumnName("academiccontrol_preinscription_average")
                      .HasColumnType("DECIMAL(4,2)");
                entity.Property(e => e.academiccontrol_preinscription_diffusionMedia)
                      .HasColumnName("academiccontrol_preinscription_diffusionMedia")
                      .HasMaxLength(150);
                entity.Property(e => e.academiccontrol_preinscription_registrationDate)
                      .HasColumnName("academiccontrol_preinscription_registrationDate")
                      .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.academiccontrol_preinscription_state)
                      .HasColumnName("academiccontrol_preinscription_state")
                      .HasMaxLength(50).HasDefaultValue("Pendiente");
                entity.Property(e => e.academiccontrol_preinscription_status)
                      .HasColumnName("academiccontrol_preinscription_status")
                      .HasDefaultValue(true);
                entity.Property(e => e.academiccontrol_preinscription_createdDate)
                      .HasColumnName("academiccontrol_preinscription_createdDate")
                      .HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.academiccontrol_preinscription_folio)
                      .IsUnique()
                      .HasFilter("[academiccontrol_preinscription_folio] IS NOT NULL");
            });

            //  PREINSCRIPCION DATOS PERSONALES
            modelBuilder.Entity<PreinscripcionDatosPersonalesEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_personaldata_table");
                entity.HasKey(e => e.academiccontrol_preinscription_personaldata_ID);
                entity.Property(e => e.academiccontrol_preinscription_personaldata_ID)
                      .HasColumnName("academiccontrol_preinscription_personaldata_ID");
                entity.Property(e => e.academiccontrol_preinscription_personaldata_preinscriptionID)
                      .HasColumnName("academiccontrol_preinscription_personaldata_preinscriptionID");
                entity.Property(e => e.academiccontrol_preinscription_personaldata_name)
                      .HasColumnName("academiccontrol_preinscription_personaldata_name")
                      .IsRequired().HasMaxLength(100);
                entity.Property(e => e.academiccontrol_preinscription_personaldata_paternalSurname)
                      .HasColumnName("academiccontrol_preinscription_personaldata_paternalSurname")
                      .IsRequired().HasMaxLength(100);
                entity.Property(e => e.academiccontrol_preinscription_personaldata_maternalSurname)
                      .HasColumnName("academiccontrol_preinscription_personaldata_maternalSurname")
                      .HasMaxLength(100);
                entity.Property(e => e.academiccontrol_preinscription_personaldata_CURP)
                      .HasColumnName("academiccontrol_preinscription_personaldata_CURP")
                      .IsRequired().HasMaxLength(18).IsFixedLength();
                entity.Property(e => e.academiccontrol_preinscription_personaldata_birthDate)
                      .HasColumnName("academiccontrol_preinscription_personaldata_birthDate");
                entity.Property(e => e.academiccontrol_preinscription_personaldata_gender)
                      .HasColumnName("academiccontrol_preinscription_personaldata_gender")
                      .IsRequired().HasMaxLength(20);
                entity.Property(e => e.academiccontrol_preinscription_personaldata_maritalStatus)
                      .HasColumnName("academiccontrol_preinscription_personaldata_maritalStatus")
                      .HasMaxLength(50);
                entity.Property(e => e.academiccontrol_preinscription_personaldata_email)
                      .HasColumnName("academiccontrol_preinscription_personaldata_email")
                      .IsRequired().HasMaxLength(200);
                entity.Property(e => e.academiccontrol_preinscription_personaldata_phone)
                      .HasColumnName("academiccontrol_preinscription_personaldata_phone")
                      .HasMaxLength(20);
                entity.Property(e => e.academiccontrol_preinscription_personaldata_status)
                      .HasColumnName("academiccontrol_preinscription_personaldata_status")
                      .HasDefaultValue(true);
                entity.Property(e => e.academiccontrol_preinscription_personaldata_createdDate)
                      .HasColumnName("academiccontrol_preinscription_personaldata_createdDate")
                      .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.DatosPersonales)
                      .HasForeignKey<PreinscripcionDatosPersonalesEntity>(
                          e => e.academiccontrol_preinscription_personaldata_preinscriptionID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //  PREINSCRIPCION DOMICILIO 
            modelBuilder.Entity<PreinscripcionDomicilioEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_address_table");
                entity.HasKey(e => e.academiccontrol_preinscription_address_ID);
                entity.Property(e => e.academiccontrol_preinscription_address_ID)
                      .HasColumnName("academiccontrol_preinscription_address_ID");
                entity.Property(e => e.academiccontrol_preinscription_address_preinscriptionID)
                      .HasColumnName("academiccontrol_preinscription_address_preinscriptionID");
                entity.Property(e => e.academiccontrol_preinscription_address_state)
                      .HasColumnName("academiccontrol_preinscription_address_state")
                      .IsRequired().HasMaxLength(100);
                entity.Property(e => e.academiccontrol_preinscription_address_municipality)
                      .HasColumnName("academiccontrol_preinscription_address_municipality")
                      .IsRequired().HasMaxLength(100);
                entity.Property(e => e.academiccontrol_preinscription_address_zipCode)
                      .HasColumnName("academiccontrol_preinscription_address_zipCode")
                      .HasMaxLength(5).IsFixedLength();
                entity.Property(e => e.academiccontrol_preinscription_address_neighborhood)
                      .HasColumnName("academiccontrol_preinscription_address_neighborhood")
                      .IsRequired().HasMaxLength(150);
                entity.Property(e => e.academiccontrol_preinscription_address_street)
                      .HasColumnName("academiccontrol_preinscription_address_street")
                      .IsRequired().HasMaxLength(200);
                entity.Property(e => e.academiccontrol_preinscription_address_exteriorNumber)
                      .HasColumnName("academiccontrol_preinscription_address_exteriorNumber")
                      .IsRequired().HasMaxLength(50);
                entity.Property(e => e.academiccontrol_preinscription_address_status)
                      .HasColumnName("academiccontrol_preinscription_address_status")
                      .HasDefaultValue(true);
                entity.Property(e => e.academiccontrol_preinscription_address_createdDate)
                      .HasColumnName("academiccontrol_preinscription_address_createdDate")
                      .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.Domicilio)
                      .HasForeignKey<PreinscripcionDomicilioEntity>(
                          e => e.academiccontrol_preinscription_address_preinscriptionID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //  PREINSCRIPCION TUTOR 
            modelBuilder.Entity<PreinscripcionTutorEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_tutor_table");
                entity.HasKey(e => e.academiccontrol_preinscription_tutor_ID);
                entity.Property(e => e.academiccontrol_preinscription_tutor_ID)
                      .HasColumnName("academiccontrol_preinscription_tutor_ID");
                entity.Property(e => e.academiccontrol_preinscription_tutor_preinscriptionID)
                      .HasColumnName("academiccontrol_preinscription_tutor_preinscriptionID");
                entity.Property(e => e.academiccontrol_preinscription_tutor_fullName)
                      .HasColumnName("academiccontrol_preinscription_tutor_fullName")
                      .IsRequired().HasMaxLength(200);
                entity.Property(e => e.academiccontrol_preinscription_tutor_relationship)
                      .HasColumnName("academiccontrol_preinscription_tutor_relationship")
                      .IsRequired().HasMaxLength(50);
                entity.Property(e => e.academiccontrol_preinscription_tutor_phone)
                      .HasColumnName("academiccontrol_preinscription_tutor_phone")
                      .IsRequired().HasMaxLength(20);
                entity.Property(e => e.academiccontrol_preinscription_tutor_status)
                      .HasColumnName("academiccontrol_preinscription_tutor_status")
                      .HasDefaultValue(true);
                entity.Property(e => e.academiccontrol_preinscription_tutor_createdDate)
                      .HasColumnName("academiccontrol_preinscription_tutor_createdDate")
                      .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.Tutor)
                      .HasForeignKey<PreinscripcionTutorEntity>(
                          e => e.academiccontrol_preinscription_tutor_preinscriptionID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //  PREINSCRIPCION ESCOLAR 
            modelBuilder.Entity<PreinscripcionEscolarEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_academic_table");
                entity.HasKey(e => e.academiccontrol_preinscription_academic_ID);
                entity.Property(e => e.academiccontrol_preinscription_academic_ID)
                      .HasColumnName("academiccontrol_preinscription_academic_ID");
                entity.Property(e => e.academiccontrol_preinscription_academic_preinscriptionID)
                      .HasColumnName("academiccontrol_preinscription_academic_preinscriptionID");
                entity.Property(e => e.academiccontrol_preinscription_academic_originSchool)
                      .HasColumnName("academiccontrol_preinscription_academic_originSchool")
                      .IsRequired().HasMaxLength(200);
                entity.Property(e => e.academiccontrol_preinscription_academic_schoolState)
                      .HasColumnName("academiccontrol_preinscription_academic_schoolState")
                      .HasMaxLength(100);
                entity.Property(e => e.academiccontrol_preinscription_academic_schoolMunicipality)
                      .HasColumnName("academiccontrol_preinscription_academic_schoolMunicipality")
                      .HasMaxLength(100);
                entity.Property(e => e.academiccontrol_preinscription_academic_CCT)
                      .HasColumnName("academiccontrol_preinscription_academic_CCT")
                      .HasMaxLength(20);
                entity.Property(e => e.academiccontrol_preinscription_academic_startDate)
                      .HasColumnName("academiccontrol_preinscription_academic_startDate");
                entity.Property(e => e.academiccontrol_preinscription_academic_endDate)
                      .HasColumnName("academiccontrol_preinscription_academic_endDate");
                entity.Property(e => e.academiccontrol_preinscription_academic_status)
                      .HasColumnName("academiccontrol_preinscription_academic_status")
                      .HasDefaultValue(true);
                entity.Property(e => e.academiccontrol_preinscription_academic_createdDate)
                      .HasColumnName("academiccontrol_preinscription_academic_createdDate")
                      .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.DatosEscolares)
                      .HasForeignKey<PreinscripcionEscolarEntity>(
                          e => e.academiccontrol_preinscription_academic_preinscriptionID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //  PREINSCRIPCION SALUD 
            modelBuilder.Entity<PreinscripcionSaludEntity>(entity =>
            {
                entity.ToTable("academiccontrol_preinscription_health_table");
                entity.HasKey(e => e.academiccontrol_preinscription_health_ID);
                entity.Property(e => e.academiccontrol_preinscription_health_ID)
                      .HasColumnName("academiccontrol_preinscription_health_ID");
                entity.Property(e => e.academiccontrol_preinscription_health_preinscriptionID)
                      .HasColumnName("academiccontrol_preinscription_health_preinscriptionID");
                entity.Property(e => e.academiccontrol_preinscription_health_medicalService)
                      .HasColumnName("academiccontrol_preinscription_health_medicalService")
                      .HasMaxLength(100);
                entity.Property(e => e.academiccontrol_preinscription_health_hasDisability)
                      .HasColumnName("academiccontrol_preinscription_health_hasDisability");
                entity.Property(e => e.academiccontrol_preinscription_health_disabilityDescription)
                      .HasColumnName("academiccontrol_preinscription_health_disabilityDescription")
                      .HasMaxLength(250);
                entity.Property(e => e.academiccontrol_preinscription_health_indigenousCommunity)
                      .HasColumnName("academiccontrol_preinscription_health_indigenousCommunity");
                entity.Property(e => e.academiccontrol_preinscription_health_indigenousCommunityDescription)
                      .HasColumnName("academiccontrol_preinscription_health_indigenousCommunityDescription")
                      .HasMaxLength(150);
                entity.Property(e => e.academiccontrol_preinscription_health_comments)
                      .HasColumnName("academiccontrol_preinscription_health_comments")
                      .HasMaxLength(500);
                entity.Property(e => e.academiccontrol_preinscription_health_hasChildren)
                      .HasColumnName("academiccontrol_preinscription_health_hasChildren");
                entity.Property(e => e.academiccontrol_preinscription_health_status)
                      .HasColumnName("academiccontrol_preinscription_health_status")
                      .HasDefaultValue(true);
                entity.Property(e => e.academiccontrol_preinscription_health_createdDate)
                      .HasColumnName("academiccontrol_preinscription_health_createdDate")
                      .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Preinscripcion)
                      .WithOne(p => p.Salud)
                      .HasForeignKey<PreinscripcionSaludEntity>(
                          e => e.academiccontrol_preinscription_health_preinscriptionID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //  INSCRIPCION 
            modelBuilder.Entity<InscripcionEntity>(entity =>
            {
                entity.ToTable("academiccontrol_inscription_table");
                entity.HasKey(e => e.academiccontrol_inscription_ID);
                entity.Property(e => e.academiccontrol_inscription_ID)
                      .HasColumnName("academiccontrol_inscription_ID");
                entity.Property(e => e.academiccontrol_inscription_preinscriptionID)
                      .HasColumnName("academiccontrol_inscription_preinscriptionID");
                entity.Property(e => e.academiccontrol_inscription_careerRequested)
                      .HasColumnName("academiccontrol_inscription_careerRequested")
                      .IsRequired().HasMaxLength(200);
                entity.Property(e => e.academiccontrol_inscription_hasTSUEnrollment)
                      .HasColumnName("academiccontrol_inscription_hasTSUEnrollment");
                entity.Property(e => e.academiccontrol_inscription_TSUEnrollment)
                      .HasColumnName("academiccontrol_inscription_TSUEnrollment")
                      .HasMaxLength(20);
                entity.Property(e => e.academiccontrol_inscription_enrollment)
                      .HasColumnName("academiccontrol_inscription_enrollment")
                      .HasMaxLength(20);
                entity.Property(e => e.academiccontrol_inscription_birthCertificatePath)
                      .HasColumnName("academiccontrol_inscription_birthCertificatePath")
                      .HasMaxLength(500);
                entity.Property(e => e.academiccontrol_inscription_curpPdfPath)
                      .HasColumnName("academiccontrol_inscription_curpPdfPath")
                      .HasMaxLength(500);
                entity.Property(e => e.academiccontrol_inscription_transcriptPath)
                      .HasColumnName("academiccontrol_inscription_transcriptPath")
                      .HasMaxLength(500);
                entity.Property(e => e.academiccontrol_inscription_registrationDate)
                      .HasColumnName("academiccontrol_inscription_registrationDate")
                      .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.academiccontrol_inscription_state)
                      .HasColumnName("academiccontrol_inscription_state")
                      .HasMaxLength(50).HasDefaultValue("Pendiente");
                entity.Property(e => e.academiccontrol_inscription_status)
                      .HasColumnName("academiccontrol_inscription_status")
                      .HasDefaultValue(true);
                entity.Property(e => e.academiccontrol_inscription_createdDate)
                      .HasColumnName("academiccontrol_inscription_createdDate")
                      .HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.academiccontrol_inscription_enrollment)
                      .IsUnique()
                      .HasFilter("[academiccontrol_inscription_enrollment] IS NOT NULL");

                entity.HasOne(e => e.Preinscripcion)
                      .WithMany()
                      .HasForeignKey(e => e.academiccontrol_inscription_preinscriptionID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.academiccontrol_inscription_actaValidada)
                      .HasColumnName("academiccontrol_inscription_actaValidada")
                      .HasDefaultValue(false);

                entity.Property(e => e.academiccontrol_inscription_curpValidado)
                      .HasColumnName("academiccontrol_inscription_curpValidado")
                      .HasDefaultValue(false);

                entity.Property(e => e.academiccontrol_inscription_boletaValidada)
                      .HasColumnName("academiccontrol_inscription_boletaValidada")
                      .HasDefaultValue(false);
            });

            //  CONFIGURACION FICHAS 
            modelBuilder.Entity<ConfiguracionFichasEntity>(entity =>
            {
                entity.ToTable("academiccontrol_inscription_ticketconfig_table");
                entity.HasKey(e => e.academiccontrol_inscription_ticketconfig_ID);
                entity.Property(e => e.academiccontrol_inscription_ticketconfig_ID)
                      .HasColumnName("academiccontrol_inscription_ticketconfig_ID");
                entity.Property(e => e.academiccontrol_inscription_ticketconfig_career)
                      .HasColumnName("academiccontrol_inscription_ticketconfig_career")
                      .IsRequired().HasMaxLength(200);
                entity.Property(e => e.academiccontrol_inscription_ticketconfig_limit)
                      .HasColumnName("academiccontrol_inscription_ticketconfig_limit");
                entity.Property(e => e.academiccontrol_inscription_ticketconfig_startDate)
                      .HasColumnName("academiccontrol_inscription_ticketconfig_startDate");
                entity.Property(e => e.academiccontrol_inscription_ticketconfig_endDate)
                      .HasColumnName("academiccontrol_inscription_ticketconfig_endDate");
                entity.Property(e => e.academiccontrol_inscription_ticketconfig_status)
                      .HasColumnName("academiccontrol_inscription_ticketconfig_status")
                      .HasDefaultValue(true);
                entity.Property(e => e.academiccontrol_inscription_ticketconfig_createdDate)
                      .HasColumnName("academiccontrol_inscription_ticketconfig_createdDate")
                      .HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.academiccontrol_inscription_ticketconfig_updatedDate)
                      .HasColumnName("academiccontrol_inscription_ticketconfig_updatedDate")
                      .HasDefaultValueSql("GETDATE()");
            });

            //  PERIODO INSCRIPCION 
            modelBuilder.Entity<PeriodoInscripcionEntity>(entity =>
            {
                entity.ToTable("academiccontrol_inscription_period_table");
                entity.HasKey(e => e.academiccontrol_inscription_period_ID);
                entity.Property(e => e.academiccontrol_inscription_period_ID)
                      .HasColumnName("academiccontrol_inscription_period_ID");
                entity.Property(e => e.academiccontrol_inscription_period_name)
                      .HasColumnName("academiccontrol_inscription_period_name")
                      .IsRequired().HasMaxLength(100);
                entity.Property(e => e.academiccontrol_inscription_period_startDate)
                      .HasColumnName("academiccontrol_inscription_period_startDate");
                entity.Property(e => e.academiccontrol_inscription_period_endDate)
                      .HasColumnName("academiccontrol_inscription_period_endDate");
                entity.Property(e => e.academiccontrol_inscription_period_status)
                      .HasColumnName("academiccontrol_inscription_period_status")
                      .HasDefaultValue(true);
                entity.Property(e => e.academiccontrol_inscription_period_createdDate)
                      .HasColumnName("academiccontrol_inscription_period_createdDate")
                      .HasDefaultValueSql("GETDATE()");
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
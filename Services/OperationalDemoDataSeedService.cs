using ControlEscolar.Data;
using ControlEscolar.Models.ManagementOperational;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Models.Operational;
using ControlEscolar.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ControlEscolar.Services;

public class OperationalDemoDataSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly OperationalSeedService _operationalSeed;
    private readonly IdentityDemoSeedService _identityDemoSeed;
    private readonly ILogger<OperationalDemoDataSeedService> _logger;

    public OperationalDemoDataSeedService(
        ApplicationDbContext context,
        OperationalSeedService operationalSeed,
        IdentityDemoSeedService identityDemoSeed,
        ILogger<OperationalDemoDataSeedService> logger)
    {
        _context = context;
        _operationalSeed = operationalSeed;
        _identityDemoSeed = identityDemoSeed;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await _operationalSeed.SeedAsync();
        await _identityDemoSeed.SeedAsync();
        await EnsureAcademicControlCatalogsAsync();
        await EnsureAcademicControlDemoDataAsync();

        var teachers = await EnsureTeachersAsync();
        var students = await EnsureStudentsAsync();

        var dualProgram = await _context.OperationalPrograms
            .FirstAsync(x => x.Status && x.IsActive && x.Type == ProgramTypes.PRACTICAS_PROFESIONALES);
        var ssProgram = await _context.OperationalPrograms
            .FirstAsync(x => x.Status && x.IsActive && x.Type == ProgramTypes.SERVICIO_SOCIAL);

        var dualOrgs = new[]
        {
            await GetOrCreateOrganizationAsync("TEMP-LABS INDUSTRIA 1", ProgramTypes.PRACTICAS_PROFESIONALES),
            await GetOrCreateOrganizationAsync("TEMP-CENTRO TECNOLOGICO 2", ProgramTypes.PRACTICAS_PROFESIONALES),
            await GetOrCreateOrganizationAsync("TEMP-PARQUE INNOVACION 3", ProgramTypes.PRACTICAS_PROFESIONALES)
        };

        var ssOrgs = new[]
        {
            await GetOrCreateOrganizationAsync("TEMP-DIF MUNICIPAL", ProgramTypes.SERVICIO_SOCIAL),
            await GetOrCreateOrganizationAsync("TEMP-BIBLIOTECA CENTRAL", ProgramTypes.SERVICIO_SOCIAL),
            await GetOrCreateOrganizationAsync("TEMP-HOSPITAL COMUNITARIO", ProgramTypes.SERVICIO_SOCIAL)
        };

        var dualAssignments = new List<OperationalStudentAssignment>();
        var ssAssignments = new List<OperationalStudentAssignment>();

        for (var i = 0; i < students.Count; i++)
        {
            var student = students[i];

            var dual = await GetOrCreateAssignmentAsync(
                studentId: student.Id,
                program: dualProgram,
                organizationId: dualOrgs[i % dualOrgs.Length].Id,
                teacherId: teachers[i % teachers.Count].Id,
                statusCode: i % 3 == 0 ? DualStatusCodes.IN_PROGRESS : DualStatusCodes.ADVISORS_ASSIGNED,
                approvedHours: i % 3 == 0 ? 220 : 40,
                createdDaysAgo: 20 + i);
            dualAssignments.Add(dual);

            var social = await GetOrCreateAssignmentAsync(
                studentId: student.Id,
                program: ssProgram,
                organizationId: ssOrgs[i % ssOrgs.Length].Id,
                teacherId: teachers[(i + 1) % teachers.Count].Id,
                statusCode: i % 4 == 0 ? SSStatusCodes.COMPLETED : SSStatusCodes.IN_PROGRESS,
                approvedHours: i % 4 == 0 ? 480 : 180,
                createdDaysAgo: 10 + i);
            ssAssignments.Add(social);
        }

        await _context.SaveChangesAsync();

        foreach (var a in dualAssignments)
        {
            await EnsureDemoDocumentAsync(a.Id, "CV_ES", "[TEMP] CV Español", DocumentStatusCodes.APPROVED, 5);
            await EnsureDemoDocumentAsync(a.Id, "SEMANA_1", "[TEMP] Reporte Semanal", DocumentStatusCodes.PENDING, 3);
        }

        foreach (var a in ssAssignments)
        {
            await EnsureDemoDocumentAsync(a.Id, "CARTA_PRESENTACION", "[TEMP] Carta Presentación", DocumentStatusCodes.APPROVED, 4);
            await EnsureDemoDocumentAsync(a.Id, "REPORTE_HORAS", "[TEMP] Reporte de Horas", DocumentStatusCodes.PENDING, 2);
        }

        await _context.SaveChangesAsync();

        var academicTempPreins = await _context.Preinscripciones.CountAsync(x => x.Folio != null && x.Folio.StartsWith("TEMP-PRE-"));
        var academicTempIns = await _context.Inscripciones.CountAsync(x => x.Matricula != null && x.Matricula.StartsWith("TEMP-MAT-"));

        _logger.LogInformation(
            "Seeder temporal completado. Students={StudentCount} Teachers={TeacherCount} DualAssignments={DualCount} SocialAssignments={SocialCount} TempPreins={PreinsCount} TempIns={InsCount}",
            students.Count,
            teachers.Count,
            dualAssignments.Count,
            ssAssignments.Count,
            academicTempPreins,
            academicTempIns);
    }

    private async Task EnsureAcademicControlCatalogsAsync()
    {
        var today = DateTime.Today;
        var careers = new[]
        {
            "INGENIERIA EN SISTEMAS",
            "MECATRONICA",
            "ADMINISTRACION",
            "CONTABILIDAD",
            "DERECHO",
            "PSICOLOGIA"
        };

        var activePeriod = await _context.PeriodosInscripcion
            .FirstOrDefaultAsync(x => x.Activo && x.FechaInicio <= today && x.FechaFin >= today);

        if (activePeriod == null)
        {
            _context.PeriodosInscripcion.Add(new PeriodoInscripcionEntity
            {
                Nombre = "Periodo Temporal Demo",
                FechaInicio = today.AddMonths(-1),
                FechaFin = today.AddMonths(2),
                Activo = true,
                FechaCreacion = DateTime.Now
            });
        }

        foreach (var career in careers)
        {
            var config = await _context.ConfiguracionFichas
                .FirstOrDefaultAsync(x => x.Carrera == career && x.Activo);

            if (config == null)
            {
                _context.ConfiguracionFichas.Add(new ConfiguracionFichasEntity
                {
                    Carrera = career,
                    LimiteFichas = 500,
                    FechaInicio = today.AddMonths(-1),
                    FechaFin = today.AddMonths(2),
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    FechaActualizacion = DateTime.Now
                });
            }
            else
            {
                config.FechaInicio = today.AddMonths(-1);
                config.FechaFin = today.AddMonths(2);
                config.Activo = true;
                config.FechaActualizacion = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task EnsureAcademicControlDemoDataAsync()
    {
        var targetPreins = 18;
        var targetIns = 12;

        var tempPreins = await _context.Preinscripciones
            .Include(x => x.DatosPersonales)
            .Include(x => x.Domicilio)
            .Include(x => x.Tutor)
            .Include(x => x.DatosEscolares)
            .Include(x => x.Salud)
            .Where(x => x.Folio != null && x.Folio.StartsWith("TEMP-PRE-"))
            .OrderBy(x => x.Id)
            .ToListAsync();

        var careers = new[]
        {
            "INGENIERIA EN SISTEMAS",
            "MECATRONICA",
            "ADMINISTRACION",
            "CONTABILIDAD",
            "DERECHO",
            "PSICOLOGIA"
        };

        while (tempPreins.Count < targetPreins)
        {
            var idx = tempPreins.Count + 1;
            var state = (idx % 6) switch
            {
                0 => "Rechazada",
                1 => "Pendiente",
                2 => "Validada",
                3 => "InscripcionHabilitada",
                4 => "Convertida",
                _ => "Pendiente"
            };

            var entity = new PreinscripcionEntity
            {
                CarreraSolicitada = careers[idx % careers.Length],
                Promedio = 7.5m + (idx % 25) / 10m,
                MedioDifusion = idx % 2 == 0 ? "Facebook" : "Feria Educativa",
                FechaPreinscripcion = DateTime.Now.AddDays(-(idx + 7)),
                EstadoPreinscripcion = state,
                DatosPersonales = new PreinscripcionDatosPersonalesEntity
                {
                    Nombre = $"TempNombre{idx}",
                    ApellidoPaterno = "Demo",
                    ApellidoMaterno = "Academic",
                    CURP = BuildTempCurp(idx),
                    FechaNacimiento = DateTime.Today.AddYears(-18).AddDays(-idx * 12),
                    Sexo = idx % 2 == 0 ? "F" : "M",
                    EstadoCivil = "Soltero",
                    Email = $"temp.preins{idx:000}@demo.local",
                    Telefono = $"771900{idx:0000}"
                },
                Domicilio = new PreinscripcionDomicilioEntity
                {
                    Estado = "Hidalgo",
                    Municipio = idx % 2 == 0 ? "Pachuca" : "Tulancingo",
                    CodigoPostal = $"43{idx % 90:000}",
                    Colonia = $"Colonia Temporal {idx}",
                    Calle = $"Calle Demo {idx}",
                    NumeroExterior = $"{10 + idx}"
                },
                Tutor = new PreinscripcionTutorEntity
                {
                    TutorNombre = $"Tutor Temporal {idx}",
                    Parentesco = idx % 2 == 0 ? "Madre" : "Padre",
                    Telefono = $"771800{idx:0000}"
                },
                DatosEscolares = new PreinscripcionEscolarEntity
                {
                    EscuelaProcedencia = $"Bachillerato Temporal {idx}",
                    EstadoEscuela = "Hidalgo",
                    MunicipioEscuela = idx % 2 == 0 ? "Pachuca" : "Mineral de la Reforma",
                    CCT = $"TMP{idx:0000000000}",
                    InicioBachillerato = DateTime.Today.AddYears(-3).AddMonths(-(idx % 8)),
                    FinBachillerato = DateTime.Today.AddMonths(-(idx % 6))
                },
                Salud = new PreinscripcionSaludEntity
                {
                    ServicioMedico = idx % 2 == 0 ? "IMSS" : "ISSSTE",
                    TieneDiscapacidad = idx % 9 == 0,
                    DiscapacidadDescripcion = idx % 9 == 0 ? "Discapacidad visual leve" : null,
                    ComunidadIndigena = idx % 7 == 0,
                    ComunidadIndigenaDescripcion = idx % 7 == 0 ? "Nahua" : null,
                    Comentarios = "Registro temporal de demostracion"
                }
            };

            _context.Preinscripciones.Add(entity);
            await _context.SaveChangesAsync();

            entity.Folio = $"TEMP-PRE-{DateTime.Now.Year}-{entity.Id:D5}";
            await _context.SaveChangesAsync();

            tempPreins.Add(entity);
        }

        var tempInscripciones = await _context.Inscripciones
            .Where(x => x.Matricula != null && x.Matricula.StartsWith("TEMP-MAT-"))
            .ToListAsync();

        var candidates = tempPreins
            .OrderBy(x => x.Id)
            .Take(targetIns)
            .ToList();

        foreach (var pre in candidates)
        {
            var existing = await _context.Inscripciones
                .FirstOrDefaultAsync(x => x.PreinscripcionId == pre.Id);

            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(existing.Matricula))
                {
                    existing.Matricula = $"TEMP-MAT-{DateTime.Now.Year}-{existing.Id:D5}";
                }
                if (string.IsNullOrWhiteSpace(existing.EstadoInscripcion))
                {
                    existing.EstadoInscripcion = "Pendiente";
                }
                if (pre.EstadoPreinscripcion != "Convertida")
                {
                    pre.EstadoPreinscripcion = "Convertida";
                }

                continue;
            }

            var idx = pre.Id % 5;
            var estadoInscripcion = idx switch
            {
                0 => "Pendiente",
                1 => "DocumentosValidados",
                2 => "PagoValidado",
                3 => "Aprobada",
                _ => "Rechazada"
            };

            var ins = new InscripcionEntity
            {
                PreinscripcionId = pre.Id,
                CarreraSolicitada = pre.CarreraSolicitada,
                TieneMatriculaTSU = pre.Id % 2 == 0,
                MatriculaTSU = pre.Id % 2 == 0 ? $"TSU-{pre.Id:D6}" : null,
                Matricula = null,
                ActaNacimientoPath = $"uploads/inscripciones/temp_acta_{pre.Id}.pdf",
                CurpPdfPath = $"uploads/inscripciones/temp_curp_{pre.Id}.pdf",
                BoletaPdfPath = $"uploads/inscripciones/temp_boleta_{pre.Id}.pdf",
                FechaInscripcion = DateTime.Now.AddDays(-(pre.Id % 14)),
                EstadoInscripcion = estadoInscripcion
            };

            _context.Inscripciones.Add(ins);
            await _context.SaveChangesAsync();

            ins.Matricula = $"TEMP-MAT-{DateTime.Now.Year}-{ins.Id:D5}";
            pre.EstadoPreinscripcion = "Convertida";
            await _context.SaveChangesAsync();

            tempInscripciones.Add(ins);
        }
    }

    private static string BuildTempCurp(int idx)
    {
        var suffix = (idx % 1000).ToString("D3");
        return $"DEMT900101HDFXX{suffix}";
    }

    private async Task EnsureAuthUsersAsync()
    {
        var roleAdminId = await GetOrCreateRoleAsync("Admin", "Acceso total al sistema");
        var roleCoordinatorId = await GetOrCreateRoleAsync("Coordinador", "Coordinacion de programas operativos");
        var roleTutorId = await GetOrCreateRoleAsync("Tutor", "Seguimiento y validacion de documentos");
        var roleAlumnoId = await GetOrCreateRoleAsync("Alumno", "Portal de estudiante");

        var defaultPassword = "Temp1234!";

        var adminUserId = await GetOrCreateUserAsync(
            username: "admin.demo",
            email: "admin.demo@demo.local",
            firstName: "Admin",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: HashPassword(defaultPassword));
        await EnsureUserRoleAsync(adminUserId, roleAdminId);

        var coordinatorUserId = await GetOrCreateUserAsync(
            username: "coordinador.demo",
            email: "coordinador.demo@demo.local",
            firstName: "Coordinador",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: HashPassword(defaultPassword));
        await EnsureUserRoleAsync(coordinatorUserId, roleCoordinatorId);

        var tutorUserId = await GetOrCreateUserAsync(
            username: "tutor.demo",
            email: "tutor.demo@demo.local",
            firstName: "Tutor",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: HashPassword(defaultPassword));
        await EnsureUserRoleAsync(tutorUserId, roleTutorId);

        var alumnoUserId = await GetOrCreateUserAsync(
            username: "alumno.demo",
            email: "alumno.demo@demo.local",
            firstName: "Alumno",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: HashPassword(defaultPassword));
        await EnsureUserRoleAsync(alumnoUserId, roleAlumnoId);

        _logger.LogInformation(
            "Usuarios iniciales de prueba asegurados. Admin={AdminUser} Coordinador={CoordinatorUser} Tutor={TutorUser} Alumno={AlumnoUser}",
            "admin.demo",
            "coordinador.demo",
            "tutor.demo",
            "alumno.demo");
    }

    private async Task<List<Student>> EnsureStudentsAsync()
    {
        var students = await _context.StudentsOperational
            .Include(x => x.Person)
            .Where(x => x.Status)
            .OrderBy(x => x.Id)
            .Take(8)
            .ToListAsync();

        if (students.Count >= 6)
        {
            return students;
        }

        var toCreate = 6 - students.Count;
        for (var i = 0; i < toCreate; i++)
        {
            var person = new Person
            {
                FirstName = $"TempAlumno{i + 1}",
                LastNamePaternal = "Prueba",
                LastNameMaternal = "DualSS",
                Email = $"temp.alumno{i + 1}@demo.local",
                Phone = $"77100010{i + 1:00}",
                Status = true
            };
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();

            var student = new Student
            {
                PersonId = person.Id,
                Matricula = $"24{i + 1:000000}",
                StatusCode = "INSCRITO",
                Status = true
            };
            _context.StudentsOperational.Add(student);
            await _context.SaveChangesAsync();

            students.Add(student);
        }

        return students.OrderBy(x => x.Id).Take(8).ToList();
    }

    private async Task<List<Teacher>> EnsureTeachersAsync()
    {
        var teachers = await _context.TeachersOperational
            .Include(x => x.Person)
            .Where(x => x.Status)
            .OrderBy(x => x.Id)
            .Take(4)
            .ToListAsync();

        if (teachers.Count >= 3)
        {
            return teachers;
        }

        var toCreate = 3 - teachers.Count;
        for (var i = 0; i < toCreate; i++)
        {
            var person = new Person
            {
                FirstName = $"TempTutor{i + 1}",
                LastNamePaternal = "Prueba",
                LastNameMaternal = "DualSS",
                Email = $"temp.tutor{i + 1}@demo.local",
                Phone = $"77100020{i + 1:00}",
                Status = true
            };
            _context.Persons.Add(person);
            await _context.SaveChangesAsync();

            var teacher = new Teacher
            {
                PersonId = person.Id,
                EmployeeNumber = $"TMPDOC{i + 1:000}",
                StatusCode = "ACTIVO",
                Status = true
            };
            _context.TeachersOperational.Add(teacher);
            await _context.SaveChangesAsync();

            teachers.Add(teacher);
        }

        return teachers.OrderBy(x => x.Id).Take(4).ToList();
    }

    private async Task<OperationalOrganization> GetOrCreateOrganizationAsync(string name, string type)
    {
        var existing = await _context.OperationalOrganizations
            .FirstOrDefaultAsync(x => x.Status && x.Name == name && x.Type == type);

        if (existing != null)
        {
            return existing;
        }

        var org = new OperationalOrganization
        {
            Name = name,
            Type = type,
            Address = "Dirección temporal de pruebas",
            ContactName = "Contacto Temporal",
            Email = "demo@temp.local",
            Phone = "7710000000",
            Status = true,
            CreatedDate = DateTime.Now
        };

        _context.OperationalOrganizations.Add(org);
        await _context.SaveChangesAsync();
        return org;
    }

    private async Task<OperationalStudentAssignment> GetOrCreateAssignmentAsync(
        int studentId,
        OperationalProgram program,
        int organizationId,
        int teacherId,
        string statusCode,
        decimal approvedHours,
        int createdDaysAgo)
    {
        var existing = await _context.OperationalStudentAssignments
            .FirstOrDefaultAsync(x => x.Status && x.StudentId == studentId && x.ProgramId == program.Id);

        if (existing != null)
        {
            existing.OrganizationId = organizationId;
            existing.TeacherId = teacherId;
            existing.StatusCode = statusCode;
            existing.ApprovedHours = approvedHours;
            existing.TotalHours = program.RequiredHours;
            return existing;
        }

        var assignment = new OperationalStudentAssignment
        {
            StudentId = studentId,
            ProgramId = program.Id,
            OrganizationId = organizationId,
            TeacherId = teacherId,
            StatusCode = statusCode,
            TotalHours = program.RequiredHours,
            ApprovedHours = approvedHours,
            Status = true,
            CreatedDate = DateTime.Now.AddDays(-createdDaysAgo)
        };

        _context.OperationalStudentAssignments.Add(assignment);
        return assignment;
    }

    private async Task EnsureDemoDocumentAsync(int assignmentId, string documentType, string title, string statusCode, int uploadDaysAgo)
    {
        var exists = await _context.OperationalDocuments
            .AnyAsync(x => x.Status && x.AssignmentId == assignmentId && x.DocumentType == documentType && x.Title == title);

        if (exists)
        {
            return;
        }

        _context.OperationalDocuments.Add(new OperationalDocument
        {
            AssignmentId = assignmentId,
            UploadedByUserId = null,
            DocumentType = documentType,
            Title = title,
            Notes = "Documento temporal generado para pruebas.",
            StatusCode = statusCode,
            UploadDate = DateTime.Now.AddDays(-uploadDaysAgo),
            CreatedDate = DateTime.Now.AddDays(-uploadDaysAgo),
            Status = true
        });
    }

    private async Task<int> GetOrCreateRoleAsync(string roleName, string? description)
    {
        var existingId = await GetScalarIntAsync(
            @"SELECT TOP 1 management_role_ID
              FROM dbo.management_role_table
              WHERE management_role_Name = @RoleName;",
            new SqlParameter("@RoleName", roleName));

        if (existingId.HasValue)
        {
            await ExecuteNonQueryAsync(
                @"UPDATE dbo.management_role_table
                  SET management_role_status = 1
                  WHERE management_role_ID = @RoleId;",
                new SqlParameter("@RoleId", existingId.Value));

            return existingId.Value;
        }

        var newRoleId = await GetScalarIntAsync(
            @"INSERT INTO dbo.management_role_table
                (management_role_Name, management_role_Description, management_role_status, management_role_createdDate)
              OUTPUT INSERTED.management_role_ID
              VALUES (@RoleName, @Description, 1, GETDATE());",
            new SqlParameter("@RoleName", roleName),
            new SqlParameter("@Description", (object?)description ?? DBNull.Value));

        if (!newRoleId.HasValue)
        {
            throw new InvalidOperationException($"No se pudo crear el rol '{roleName}'.");
        }

        return newRoleId.Value;
    }

    private async Task<int> GetOrCreateUserAsync(
        string username,
        string email,
        string firstName,
        string lastNamePaternal,
        string? lastNameMaternal,
        string passwordHash)
    {
        var existingUserId = await GetScalarIntAsync(
            @"SELECT TOP 1 management_user_ID
              FROM dbo.management_user_table
              WHERE management_user_Username = @Username;",
            new SqlParameter("@Username", username));

        var personId = await GetOrCreateManagementPersonAsync(
            email,
            firstName,
            lastNamePaternal,
            lastNameMaternal);

        if (existingUserId.HasValue)
        {
            await ExecuteNonQueryAsync(
                @"UPDATE dbo.management_user_table
                  SET management_user_Email = @Email,
                      management_user_PersonID = COALESCE(management_user_PersonID, @PersonID),
                      management_user_IsLocked = 0,
                      management_user_LockReason = NULL,
                      management_user_status = 1
                  WHERE management_user_ID = @UserId;",
                new SqlParameter("@Email", email),
                new SqlParameter("@PersonID", personId),
                new SqlParameter("@UserId", existingUserId.Value));

            return existingUserId.Value;
        }

        var newUserId = await GetScalarIntAsync(
            @"INSERT INTO dbo.management_user_table
                (management_user_PersonID, management_user_Username, management_user_Email, management_user_PasswordHash, management_user_IsLocked, management_user_status, management_user_createdDate)
              OUTPUT INSERTED.management_user_ID
              VALUES (@PersonID, @Username, @Email, @PasswordHash, 0, 1, GETDATE());",
            new SqlParameter("@PersonID", personId),
            new SqlParameter("@Username", username),
            new SqlParameter("@Email", email),
            new SqlParameter("@PasswordHash", passwordHash));

        if (!newUserId.HasValue)
        {
            throw new InvalidOperationException($"No se pudo crear el usuario '{username}'.");
        }

        return newUserId.Value;
    }

    private async Task<int> GetOrCreateManagementPersonAsync(string email, string firstName, string lastNamePaternal, string? lastNameMaternal)
    {
        var existingPersonId = await GetScalarIntAsync(
            @"SELECT TOP 1 management_person_ID
              FROM dbo.management_person_table
              WHERE management_person_Email = @Email
              ORDER BY management_person_ID;",
            new SqlParameter("@Email", email));

        if (existingPersonId.HasValue)
        {
            await ExecuteNonQueryAsync(
                @"UPDATE dbo.management_person_table
                  SET management_person_FirstName = @FirstName,
                      management_person_LastNamePaternal = @LastNamePaternal,
                      management_person_LastNameMaternal = @LastNameMaternal,
                      management_person_status = 1
                  WHERE management_person_ID = @PersonId;",
                new SqlParameter("@FirstName", firstName),
                new SqlParameter("@LastNamePaternal", lastNamePaternal),
                new SqlParameter("@LastNameMaternal", (object?)lastNameMaternal ?? DBNull.Value),
                new SqlParameter("@PersonId", existingPersonId.Value));

            return existingPersonId.Value;
        }

        var newPersonId = await GetScalarIntAsync(
            @"INSERT INTO dbo.management_person_table
                (management_person_FirstName, management_person_LastNamePaternal, management_person_LastNameMaternal, management_person_Email, management_person_status, management_person_createdDate)
              OUTPUT INSERTED.management_person_ID
              VALUES (@FirstName, @LastNamePaternal, @LastNameMaternal, @Email, 1, GETDATE());",
            new SqlParameter("@FirstName", firstName),
            new SqlParameter("@LastNamePaternal", lastNamePaternal),
            new SqlParameter("@LastNameMaternal", (object?)lastNameMaternal ?? DBNull.Value),
            new SqlParameter("@Email", email));

        if (!newPersonId.HasValue)
        {
            throw new InvalidOperationException($"No se pudo crear la persona para '{email}'.");
        }

        return newPersonId.Value;
    }

    private async Task EnsureUserRoleAsync(int userId, int roleId)
    {
        var userRoleId = await GetScalarIntAsync(
            @"SELECT TOP 1 management_userrole_ID
              FROM dbo.management_userrole_table
              WHERE management_userrole_UserID = @UserId
                AND management_userrole_RoleID = @RoleId;",
            new SqlParameter("@UserId", userId),
            new SqlParameter("@RoleId", roleId));

        if (userRoleId.HasValue)
        {
            await ExecuteNonQueryAsync(
                @"UPDATE dbo.management_userrole_table
                  SET management_userrole_status = 1
                  WHERE management_userrole_ID = @UserRoleId;",
                new SqlParameter("@UserRoleId", userRoleId.Value));
            return;
        }

        await ExecuteNonQueryAsync(
            @"INSERT INTO dbo.management_userrole_table
                (management_userrole_UserID, management_userrole_RoleID, management_userrole_status, management_userrole_createdDate)
              VALUES (@UserId, @RoleId, 1, GETDATE());",
            new SqlParameter("@UserId", userId),
            new SqlParameter("@RoleId", roleId));
    }

    private async Task<int?> GetScalarIntAsync(string sql, params SqlParameter[] parameters)
    {
        var conn = _context.Database.GetDbConnection();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;

        foreach (var parameter in parameters)
        {
            cmd.Parameters.Add(parameter);
        }

        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync();
        }

        try
        {
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(result);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private async Task ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
    {
        var conn = _context.Database.GetDbConnection();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;

        foreach (var parameter in parameters)
        {
            cmd.Parameters.Add(parameter);
        }

        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync();
        }

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
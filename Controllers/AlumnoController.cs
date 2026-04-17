using ControlEscolar.Data;
using ControlEscolar.Models;
using ControlEscolar.Models.ManagementOperational;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Models.Operational;
using ControlEscolar.Services;
using ControlEscolar.ViewModels.StudentPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Alumno,Student,STUDENT,ADMIN,Admin")]
    public class AlumnoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IModuleFlowConfigurationService _moduleFlowConfigurationService;

        // Inyectamos la base de datos
        public AlumnoController(ApplicationDbContext context, IModuleFlowConfigurationService moduleFlowConfigurationService)
        {
            _context = context;
            _moduleFlowConfigurationService = moduleFlowConfigurationService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Inicio";
            ViewData["ShowBackButton"] = false;

            var student = await GetCurrentStudentAsync();
            var currentCuatrimestre = await _moduleFlowConfigurationService.ExtractCuatrimestreFromGroupCodeAsync(student?.Group?.Code);

            var dualAccess = await _moduleFlowConfigurationService.BuildAccessAsync(
                ProgramTypes.PRACTICAS_PROFESIONALES,
                currentCuatrimestre,
                null);

            var socialAccess = await _moduleFlowConfigurationService.BuildAccessAsync(
                ProgramTypes.SERVICIO_SOCIAL,
                currentCuatrimestre,
                null);

            ViewBag.StudentName = student?.Person.FullName ?? "Alumno";
            ViewBag.StudentCareer = student?.Career?.Name ?? "Carrera no disponible";
            ViewBag.StudentMatricula = string.IsNullOrWhiteSpace(student?.Matricula) ? "SIN MATRÍCULA" : student!.Matricula;
            ViewBag.CurrentCuatrimestre = currentCuatrimestre;
            ViewBag.CanShowDualPortal = dualAccess.CanAccessPortal;
            ViewBag.CanShowSocialPortal = socialAccess.CanAccessPortal;

            return View();
        }

        public IActionResult Entrevista()
        {
            ViewData["Title"] = "Entrevista Inicial";
            ViewData["ShowBackButton"] = true;
            return View();
        }

        public IActionResult Calificaciones()
        {
            ViewData["Title"] = "Kardex";
            ViewData["ShowBackButton"] = true;
            return View();
        }

        public IActionResult Asistencias()
        {
            ViewData["Title"] = "Mis Asistencias";
            ViewData["ShowBackButton"] = true;
            return View();
        }

        public async Task<IActionResult> ModeloDual()
        {
            ViewData["Title"] = "Modelo DUAL";
            ViewData["ShowBackButton"] = true;

            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
                return View(new DualPortalViewModel
                {
                    PortalAvailabilityMessage = "No fue posible cargar la información del alumno autenticado."
                });
            }

            var currentCuatrimestre = await _moduleFlowConfigurationService.ExtractCuatrimestreFromGroupCodeAsync(student.Group?.Code);

            var assignment = await _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Program)
                .Include(x => x.Organization)
                .Include(x => x.Teacher)
                    .ThenInclude(t => t!.Person)
                .Where(x => x.Status
                    && x.StudentId == student.Id
                    && x.Program.Type == ProgramTypes.PRACTICAS_PROFESIONALES)
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefaultAsync();

            var documents = assignment == null
                ? new List<OperationalDocument>()
                : await _context.OperationalDocuments
                    .AsNoTracking()
                    .Where(x => x.Status && x.AssignmentId == assignment.Id)
                    .OrderByDescending(x => x.UploadDate)
                    .ToListAsync();

            var organizations = await _context.OperationalOrganizations
                .AsNoTracking()
                .Where(x => x.Status && x.Type == ProgramTypes.PRACTICAS_PROFESIONALES)
                .OrderBy(x => x.Name)
                .Select(x => new StudentPortalOrganizationOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Address = string.IsNullOrWhiteSpace(x.Address) ? "Sin direccion" : x.Address,
                    ContactName = string.IsNullOrWhiteSpace(x.ContactName) ? "Sin contacto" : x.ContactName,
                    Email = string.IsNullOrWhiteSpace(x.Email) ? "Sin correo" : x.Email,
                    Phone = string.IsNullOrWhiteSpace(x.Phone) ? "Sin telefono" : x.Phone,
                })
                .ToListAsync();

            var trackedHours = assignment?.ApprovedHours > 0m
                ? assignment.ApprovedHours
                : assignment?.TotalHours ?? 0m;
            var requiredHours = assignment?.Program.RequiredHours ?? 480m;
            var progress = requiredHours > 0m
                ? Math.Round((trackedHours / requiredHours) * 100m, 1)
                : 0m;

            var (businessName, businessRole, businessEmail) = ParseBusinessAdvisor(assignment?.EvaluationNotes);
            var profileMetadata = ParseMetadata(assignment?.EvaluationNotes);
            var access = await _moduleFlowConfigurationService.BuildAccessAsync(
                ProgramTypes.PRACTICAS_PROFESIONALES,
                currentCuatrimestre,
                assignment?.StatusCode);

            var model = new DualPortalViewModel
            {
                StudentId = student.Id,
                CurrentCuatrimestre = currentCuatrimestre,
                CanAccessPortal = access.CanAccessPortal,
                CanAccessTrackingStage = access.CanAccessTracking,
                PortalStartCuatrimestre = access.PortalStartCuatrimestre,
                TrackingStartCuatrimestre = access.TrackingStartCuatrimestre,
                ShowStep1 = access.IsStepVisible("PASO1"),
                ShowStep2 = access.IsStepVisible("PASO2"),
                ShowStep3 = access.IsStepVisible("PASO3"),
                ShowStep4 = access.IsStepVisible("PASO4"),
                ShowStep5 = access.IsStepVisible("PASO5"),
                ShowStep6 = access.IsStepVisible("PASO6"),
                PortalAvailabilityMessage = access.CanAccessPortal
                    ? string.Empty
                    : $"El módulo dual se habilita a partir del cuatrimestre {access.PortalStartCuatrimestre} y solo si ese cuatrimestre está activo en catálogo.",
                FullName = student.Person.FullName,
                Matricula = string.IsNullOrWhiteSpace(student.Matricula) ? "S/N" : student.Matricula,
                Curp = string.IsNullOrWhiteSpace(student.Person.CURP) ? "N/D" : student.Person.CURP,
                CareerName = student.Career?.Name ?? "Sin carrera",
                GroupCode = string.IsNullOrWhiteSpace(student.Group?.Code) ? "Sin grupo" : student.Group.Code,
                Shift = string.IsNullOrWhiteSpace(student.Group?.Shift) ? "Sin turno" : student.Group.Shift,
                Email = string.IsNullOrWhiteSpace(student.Person.Email) ? "Sin correo" : student.Person.Email,
                Phone = string.IsNullOrWhiteSpace(student.Person.Phone) ? "Sin telefono" : student.Person.Phone,
                Grade = currentCuatrimestre?.ToString() ?? string.Empty,
                InternshipPeriod = TryGetString(profileMetadata, "Perfil.PeriodoEstadias") ?? string.Empty,
                InternshipYear = TryGetInt(profileMetadata, "Perfil.AnioEstadias"),
                AssignmentId = assignment?.Id ?? 0,
                AssignmentStatusCode = assignment?.StatusCode ?? DualStatusCodes.REGISTERED,
                OrganizationId = assignment?.OrganizationId ?? 0,
                OrganizationName = assignment?.Organization?.Name ?? "Sin empresa",
                OrganizationContact = assignment?.Organization?.ContactName ?? "Sin contacto",
                AcademicAdvisorName = assignment?.Teacher?.Person?.FullName ?? "Sin asignar",
                AcademicAdvisorEmail = string.IsNullOrWhiteSpace(assignment?.Teacher?.Person?.Email) ? "Sin correo" : assignment!.Teacher!.Person.Email!,
                AcademicAdvisorPhone = string.IsNullOrWhiteSpace(assignment?.Teacher?.Person?.Phone) ? "Sin telefono" : assignment!.Teacher!.Person.Phone!,
                BusinessAdvisorName = businessName,
                BusinessAdvisorRole = businessRole,
                BusinessAdvisorEmail = businessEmail,
                ApprovedHours = trackedHours,
                RequiredHours = requiredHours,
                ProgressPercent = progress,
                RequestedLettersCount = documents.Count(x => x.DocumentType == "PRESENTATION_LETTER"),
                HasResumeSpanish = documents.Any(x => x.DocumentType == "RESUME_SPANISH"),
                HasResumeEnglish = documents.Any(x => x.DocumentType == "RESUME_ENGLISH"),
                HasImssCertificate = documents.Any(x => x.DocumentType == "IMSS_CERTIFICATE"),
                HasAcceptanceLetter = documents.Any(x => x.DocumentType == "ACCEPTANCE_LETTER"),
                AvailableOrganizations = organizations,
                WeeklyReports = documents.Where(x => x.DocumentType == "WEEKLY_REPORT").Select(MapDocument).ToList(),
                Documents = documents.Select(MapDocument).ToList(),
            };

            return View(model);
        }

        public async Task<IActionResult> ServicioSocial()
        {
            ViewData["Title"] = "Servicio Social";
            ViewData["ShowBackButton"] = true;

            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
                return View(new SocialServicePortalViewModel
                {
                    PortalAvailabilityMessage = "No fue posible cargar la información del alumno autenticado."
                });
            }

            var currentCuatrimestre = await _moduleFlowConfigurationService.ExtractCuatrimestreFromGroupCodeAsync(student.Group?.Code);

            var assignment = await _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Program)
                .Include(x => x.Organization)
                .Include(x => x.Teacher)
                    .ThenInclude(t => t!.Person)
                .Where(x => x.Status
                    && x.StudentId == student.Id
                    && x.Program.Type == ProgramTypes.SERVICIO_SOCIAL)
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefaultAsync();

            var documents = assignment == null
                ? new List<OperationalDocument>()
                : await _context.OperationalDocuments
                    .AsNoTracking()
                    .Where(x => x.Status && x.AssignmentId == assignment.Id)
                    .OrderByDescending(x => x.UploadDate)
                    .ToListAsync();

            var organizations = await _context.OperationalOrganizations
                .AsNoTracking()
                .Where(x => x.Status && x.Type == ProgramTypes.SERVICIO_SOCIAL)
                .OrderBy(x => x.Name)
                .Select(x => new StudentPortalOrganizationOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Address = string.IsNullOrWhiteSpace(x.Address) ? "Sin direccion" : x.Address,
                    ContactName = string.IsNullOrWhiteSpace(x.ContactName) ? "Sin contacto" : x.ContactName,
                    Email = string.IsNullOrWhiteSpace(x.Email) ? "Sin correo" : x.Email,
                    Phone = string.IsNullOrWhiteSpace(x.Phone) ? "Sin telefono" : x.Phone,
                })
                .ToListAsync();

            var requiredHours = assignment?.Program.RequiredHours ?? 480m;
            var totalHours = assignment?.TotalHours ?? 0m;
            var progress = requiredHours > 0m
                ? Math.Round((totalHours / requiredHours) * 100m, 1)
                : 0m;
            var profileMetadata = ParseMetadata(assignment?.EvaluationNotes);
            var access = await _moduleFlowConfigurationService.BuildAccessAsync(
                ProgramTypes.SERVICIO_SOCIAL,
                currentCuatrimestre,
                assignment?.StatusCode);

            var model = new SocialServicePortalViewModel
            {
                StudentId = student.Id,
                CurrentCuatrimestre = currentCuatrimestre,
                CanAccessPortal = access.CanAccessPortal,
                CanAccessTrackingStage = access.CanAccessTracking,
                PortalStartCuatrimestre = access.PortalStartCuatrimestre,
                TrackingStartCuatrimestre = access.TrackingStartCuatrimestre,
                ShowStep1 = access.IsStepVisible("PASO1"),
                ShowStep2 = access.IsStepVisible("PASO2"),
                ShowStep3 = access.IsStepVisible("PASO3"),
                ShowStep4 = access.IsStepVisible("PASO4"),
                ShowStep5 = access.IsStepVisible("PASO5"),
                ShowStep6 = access.IsStepVisible("PASO6"),
                PortalAvailabilityMessage = access.CanAccessPortal
                    ? string.Empty
                    : $"El servicio social se habilita a partir del cuatrimestre {access.PortalStartCuatrimestre} y solo si ese cuatrimestre está activo en catálogo.",
                FullName = student.Person.FullName,
                Matricula = string.IsNullOrWhiteSpace(student.Matricula) ? "S/N" : student.Matricula,
                Curp = string.IsNullOrWhiteSpace(student.Person.CURP) ? "N/D" : student.Person.CURP,
                CareerName = student.Career?.Name ?? "Sin carrera",
                GroupCode = string.IsNullOrWhiteSpace(student.Group?.Code) ? "Sin grupo" : student.Group.Code,
                Shift = string.IsNullOrWhiteSpace(student.Group?.Shift) ? "Sin turno" : student.Group.Shift,
                Email = string.IsNullOrWhiteSpace(student.Person.Email) ? "Sin correo" : student.Person.Email,
                Phone = string.IsNullOrWhiteSpace(student.Person.Phone) ? "Sin telefono" : student.Person.Phone,
                Grade = currentCuatrimestre?.ToString() ?? string.Empty,
                ServicePeriod = TryGetString(profileMetadata, "Perfil.PeriodoServicio") ?? string.Empty,
                ServiceYear = TryGetInt(profileMetadata, "Perfil.AnioServicio"),
                AssignmentId = assignment?.Id ?? 0,
                AssignmentStatusCode = assignment?.StatusCode ?? SSStatusCodes.REGISTERED,
                OrganizationId = assignment?.OrganizationId ?? 0,
                OrganizationName = assignment?.Organization?.Name ?? "Sin institucion",
                OrganizationContact = assignment?.Organization?.ContactName ?? "Sin contacto",
                AcademicAdvisorName = assignment?.Teacher?.Person?.FullName ?? "Sin asignar",
                AcademicAdvisorEmail = string.IsNullOrWhiteSpace(assignment?.Teacher?.Person?.Email) ? "Sin correo" : assignment!.Teacher!.Person.Email!,
                AcademicAdvisorPhone = string.IsNullOrWhiteSpace(assignment?.Teacher?.Person?.Phone) ? "Sin telefono" : assignment!.Teacher!.Person.Phone!,
                TotalHours = totalHours,
                RequiredHours = requiredHours,
                ProgressPercent = progress,
                EvaluationScore = assignment?.EvaluationScore,
                EvaluationNotes = assignment?.EvaluationNotes,
                IsReleased = string.Equals(assignment?.StatusCode, SSStatusCodes.RELEASED, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(assignment?.StatusCode, SSStatusCodes.COMPLETED, StringComparison.OrdinalIgnoreCase),
                RequestedLettersCount = documents.Count(x => x.DocumentType == "PRESENTATION_LETTER"),
                HasAcceptanceLetter = documents.Any(x => x.DocumentType == "ACCEPTANCE_LETTER"),
                AvailableOrganizations = organizations,
                WeeklyReports = documents.Where(x => x.DocumentType == "WEEKLY_REPORT").Select(MapDocument).ToList(),
                HourLogs = documents.Where(x => x.DocumentType == "HOURS_LOG").Select(MapDocument).ToList(),
                Documents = documents.Select(MapDocument).ToList(),
            };

            return View(model);
        }

        // ====================================================
        // AQUÍ CARGAMOS EL HISTORIAL DEL ALUMNO
        // ====================================================
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [Authorize(Roles = "STUDENT, ADMIN")]
        public IActionResult Tramites()
        {
            ViewData["Title"] = "Trámites Escolares";
            ViewData["ShowBackButton"] = true;

            // Buscamos el ID de forma segura con la lógica "todoterreno"
            var userIdClaim = User.FindFirst("UserId")?.Value
                              ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            int userIdActual = int.TryParse(userIdClaim, out int id) ? id : 0;

            if (userIdActual == 0) return View(new List<DetalleSolicitudViewModel>());

            var historial = _context.Set<DetalleSolicitudViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_solicitud_getbyalumno', @ID={userIdActual}")
                .AsEnumerable()
                .ToList();

            return View(historial);
        }

        private async Task<Student?> GetCurrentStudentAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            var user = await _context.ManagementUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.management_user_ID == userId && x.management_user_status);

            if (user?.management_user_PersonID == null)
            {
                return null;
            }

            return await _context.StudentsOperational
                .AsNoTracking()
                .Include(x => x.Person)
                .Include(x => x.Career)
                .Include(x => x.Group)
                .FirstOrDefaultAsync(x => x.PersonId == user.management_user_PersonID.Value && x.Status);
        }

        private static StudentPortalDocumentRowViewModel MapDocument(OperationalDocument document)
        {
            var metadata = ParseMetadata(document.Notes);

            return new StudentPortalDocumentRowViewModel
            {
                Title = document.Title,
                DocumentType = document.DocumentType,
                StatusCode = string.IsNullOrWhiteSpace(document.StatusCode) ? DocumentStatusCodes.PENDING : document.StatusCode,
                UploadDate = document.UploadDate,
                FilePath = document.FilePath,
                Notes = document.Notes,
                ReviewComments = document.ReviewComments,
                ReviewDate = document.ReviewDate,
                WeekNumber = TryGetInt(metadata, "Semana"),
                HoursWorked = TryGetDecimal(metadata, "Horas"),
                Feedback = TryGetString(metadata, "Feedback")
                    ?? document.ReviewComments
                    ?? TryGetString(metadata, "Retroalimentacion")
                    ?? string.Empty,
            };
        }

        private static Dictionary<string, string> ParseMetadata(string? notes)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(notes))
            {
                return result;
            }

            foreach (var part in notes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separatorIndex = part.IndexOf(':');
                if (separatorIndex <= 0 || separatorIndex >= part.Length - 1)
                {
                    continue;
                }

                var key = part[..separatorIndex].Trim();
                var value = part[(separatorIndex + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    result[key] = value;
                }
            }

            return result;
        }

        private static int? TryGetInt(IReadOnlyDictionary<string, string> metadata, string key)
        {
            return metadata.TryGetValue(key, out var value) && int.TryParse(value, out var number)
                ? number
                : null;
        }

        private static decimal? TryGetDecimal(IReadOnlyDictionary<string, string> metadata, string key)
        {
            return metadata.TryGetValue(key, out var value)
                && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
                ? number
                : null;
        }

        private static string? TryGetString(IReadOnlyDictionary<string, string> metadata, string key)
        {
            return metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
        }

        private static (string Name, string Role, string Email) ParseBusinessAdvisor(string? notes)
        {
            var metadata = ParseMetadata(notes);

            var name = TryGetString(metadata, "Asesor empresarial") ?? "Sin registrar";
            var role = TryGetString(metadata, "Cargo") ?? "N/D";
            var email = TryGetString(metadata, "Email") ?? "N/D";

            return (name, role, email);
        }

    }
}
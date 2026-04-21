using ControlEscolar.Data;
using ControlEscolar.Models.ManagementOperational;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Models.Operational;
using ControlEscolar.ViewModels.OperationalTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "ASESORACADEMICO,AsesorAcademico,ASESOR ACADEMICO,Asesor academico,COORDINADOR,Coordinador,COORDINADORDUAL,CoordinadorDual,COORDINADORMODULODUAL,COORDINADORSERVICIOSOCIAL,COORDINADOR SERVICIO SOCIAL,Coordinador servicio social,CoordinadorServicioSocial,COORDINADORDESERVICIOSOCIAL,ADMIN,Admin")]
    public class AsesorAcademicoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AsesorAcademicoController> _logger;

        public AsesorAcademicoController(ApplicationDbContext context, ILogger<AsesorAcademicoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /AsesorAcademico/AlumnosAsignados
        public async Task<IActionResult> AlumnosAsignados(string? search, string? programa, string? module = null)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var roleScopedModule = GetRoleScopedModule();
            ViewData["Title"] = isGlobalView ? "Estudiantes de Asesoría Académica" : "Mis Estudiantes Asignados";

            Teacher? teacher = null;
            int? teacherId = null;
            if (!isGlobalView)
            {
                teacher = await GetCurrentTeacherAsync();
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "No se encontró el perfil de docente/tutor.";
                    ViewBag.SearchText = search ?? "";
                    ViewBag.ProgramaFilter = programa ?? "";
                    ViewBag.ModuleFilter = module ?? "";
                    ViewBag.IsGlobalAdvisorView = false;
                    return View(new List<AsesorAlumnoRowViewModel>());
                }

                teacherId = teacher.Id;
            }

            var query = _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Student).ThenInclude(x => x.Person)
                .Include(x => x.Student).ThenInclude(x => x.Career)
                .Include(x => x.Teacher!).ThenInclude(x => x.Person)
                .Include(x => x.Program)
                .Include(x => x.Organization)
                .Where(x => x.Status);

            if (!isGlobalView)
                query = query.Where(x => x.TeacherId == teacherId);

            query = FilterAssignmentsByRoleScope(query);

            var requestedModule = NormalizeRequestedModule(module);
            if (!string.IsNullOrWhiteSpace(requestedModule))
                query = query.Where(x => x.Program.Type == requestedModule);

            if (!string.IsNullOrWhiteSpace(programa))
                query = query.Where(x => x.Program.Type == programa);

            var assignments = await query.OrderByDescending(x => x.CreatedDate).ToListAsync();

            var rows = assignments.Select(a =>
            {
                var name = a.Student.Person.FullName;
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initials = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}"
                    : name.Length > 0 ? name[0].ToString() : "?";
                return new AsesorAlumnoRowViewModel
                {
                    AssignmentId = a.Id,
                    StudentName = name,
                    Initials = initials.ToUpper(),
                    Matricula = a.Student.Matricula ?? "N/A",
                    AdvisorName = a.Teacher?.Person?.FullName ?? "Sin asesor",
                    ProgramName = a.Program.Name,
                    ProgramType = a.Program.Type,
                    OrganizationName = a.Organization?.Name ?? "Sin organización",
                    StatusCode = a.StatusCode,
                    ApprovedHours = a.ApprovedHours,
                    RequiredHours = a.Program.RequiredHours,
                    IsEvaluated = a.EvaluationScore.HasValue,
                    CreatedDate = a.CreatedDate,
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                rows = rows.Where(r =>
                    r.StudentName.ToLower().Contains(s) ||
                    r.Matricula.ToLower().Contains(s)).ToList();
            }

            ViewBag.SearchText = search ?? "";
            ViewBag.ProgramaFilter = programa ?? "";
            ViewBag.ModuleFilter = requestedModule ?? "";
            ViewBag.RoleScopedModule = roleScopedModule ?? "";
            ViewBag.IsGlobalAdvisorView = isGlobalView;
            return View(rows);
        }

        // GET: /AsesorAcademico/RevisionDocumentos
        public async Task<IActionResult> RevisionDocumentos(string? search, string? estado, string? module = null)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var roleScopedModule = GetRoleScopedModule();
            ViewData["Title"] = isGlobalView ? "Documentación de Asesoría Académica" : "Validación de Documentación";

            Teacher? teacher = null;
            int? teacherId = null;
            if (!isGlobalView)
            {
                teacher = await GetCurrentTeacherAsync();
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "No se encontró el perfil de docente/tutor.";
                    ViewBag.SearchText = "";
                    ViewBag.EstadoFilter = "";
                    ViewBag.ModuleFilter = module ?? "";
                    ViewBag.PendingCount = 0;
                    ViewBag.IsGlobalAdvisorView = false;
                    return View(new List<AsesorDocumentoRowViewModel>());
                }

                teacherId = teacher.Id;
            }

            var docQuery = _context.OperationalDocuments
                .AsNoTracking()
                .Include(d => d.Assignment)
                    .ThenInclude(a => a.Teacher!)
                        .ThenInclude(t => t.Person)
                .Include(d => d.Assignment)
                    .ThenInclude(a => a.Program)
                .Include(d => d.Assignment)
                    .ThenInclude(a => a.Student)
                        .ThenInclude(s => s.Person)
                .Include(d => d.Assignment)
                    .ThenInclude(a => a.Student)
                .Where(d => d.Status);

            if (!isGlobalView)
                docQuery = docQuery.Where(d => d.Assignment.TeacherId == teacherId);

            docQuery = FilterDocumentsByRoleScope(docQuery);

            var requestedModule = NormalizeRequestedModule(module);
            if (!string.IsNullOrWhiteSpace(requestedModule))
                docQuery = docQuery.Where(d => d.Assignment.Program.Type == requestedModule);

            if (!string.IsNullOrWhiteSpace(estado))
                docQuery = docQuery.Where(d => d.StatusCode == estado);

            var docs = await docQuery.OrderByDescending(d => d.UploadDate).ToListAsync();

            var rows = docs.Select(d => new AsesorDocumentoRowViewModel
            {
                DocumentId = d.Id,
                AssignmentId = d.AssignmentId,
                StudentName = d.Assignment.Student.Person.FullName,
                Matricula = d.Assignment.Student.Matricula ?? "N/A",
                AdvisorName = d.Assignment.Teacher?.Person?.FullName ?? "Sin asesor",
                ProgramType = d.Assignment.Program.Type,
                Title = d.Title,
                DocumentType = d.DocumentType,
                FilePath = d.FilePath,
                UploadDate = d.UploadDate,
                StatusCode = d.StatusCode,
                ReviewComments = d.ReviewComments,
            }).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                rows = rows.Where(r =>
                    r.StudentName.ToLower().Contains(s) ||
                    r.Matricula.ToLower().Contains(s)).ToList();
            }

            ViewBag.SearchText = search ?? "";
            ViewBag.EstadoFilter = estado ?? "";
            ViewBag.ModuleFilter = requestedModule ?? "";
            ViewBag.RoleScopedModule = roleScopedModule ?? "";
            ViewBag.IsGlobalAdvisorView = isGlobalView;
            ViewBag.PendingCount = rows.Count(r => r.StatusCode == DocumentStatusCodes.PENDING);
            return View(rows);
        }

        // POST: /AsesorAcademico/AprobarDocumento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarDocumento(int documentId)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var teacher = await GetCurrentTeacherAsync();
            if (!isGlobalView && teacher == null)
            {
                TempData["ErrorMessage"] = "Perfil de docente no encontrado.";
                return RedirectToAction(nameof(RevisionDocumentos));
            }

            var doc = await GetDocumentForAccessScopeAsync(documentId, teacher?.Id, isGlobalView);
            if (doc == null)
            {
                TempData["ErrorMessage"] = "Documento no encontrado.";
                return RedirectToAction(nameof(RevisionDocumentos));
            }

            if (doc.StatusCode != DocumentStatusCodes.PENDING)
            {
                TempData["ErrorMessage"] = "Solo se pueden aprobar documentos pendientes.";
                return RedirectToAction(nameof(RevisionDocumentos));
            }

            doc.StatusCode = DocumentStatusCodes.APPROVED;
            if (teacher != null)
                doc.ReviewedByTeacherId = teacher.Id;
            doc.ReviewDate = DateTime.Now;
            doc.ReviewComments = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("AsesorAcademico AprobarDocumento TeacherId={T} DocId={D}", teacher?.Id, documentId);
            TempData["SuccessMessage"] = "Documento aprobado correctamente.";
            return RedirectToAction(nameof(RevisionDocumentos));
        }

        // POST: /AsesorAcademico/RechazarDocumento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarDocumento(int documentId, string? comments)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var teacher = await GetCurrentTeacherAsync();
            if (!isGlobalView && teacher == null)
            {
                TempData["ErrorMessage"] = "Perfil de docente no encontrado.";
                return RedirectToAction(nameof(RevisionDocumentos));
            }

            var doc = await GetDocumentForAccessScopeAsync(documentId, teacher?.Id, isGlobalView);
            if (doc == null)
            {
                TempData["ErrorMessage"] = "Documento no encontrado.";
                return RedirectToAction(nameof(RevisionDocumentos));
            }

            if (doc.StatusCode != DocumentStatusCodes.PENDING)
            {
                TempData["ErrorMessage"] = "Solo se pueden rechazar documentos pendientes.";
                return RedirectToAction(nameof(RevisionDocumentos));
            }

            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] = "Debes capturar comentarios para rechazar un documento.";
                return RedirectToAction(nameof(RevisionDocumentos));
            }

            doc.StatusCode = DocumentStatusCodes.REJECTED;
            if (teacher != null)
                doc.ReviewedByTeacherId = teacher.Id;
            doc.ReviewDate = DateTime.Now;
            doc.ReviewComments = comments.Trim();
            await _context.SaveChangesAsync();

            _logger.LogInformation("AsesorAcademico RechazarDocumento TeacherId={T} DocId={D}", teacher?.Id, documentId);
            TempData["SuccessMessage"] = "Retroalimentación enviada al estudiante.";
            return RedirectToAction(nameof(RevisionDocumentos));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarHorasPendientes(int assignmentId)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var teacher = await GetCurrentTeacherAsync();
            int? teacherId = teacher?.Id;

            if (!isGlobalView && teacher == null)
            {
                TempData["ErrorMessage"] = "Perfil de docente no encontrado.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var assignmentQuery = _context.OperationalStudentAssignments
                .Include(x => x.Program)
                .Include(x => x.Documents)
                .Where(x => x.Id == assignmentId && x.Status);

            if (!isGlobalView)
                assignmentQuery = assignmentQuery.Where(x => x.TeacherId == teacherId);

            assignmentQuery = FilterAssignmentsByRoleScope(assignmentQuery);

            var assignment = await assignmentQuery.FirstOrDefaultAsync();
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Asignación no encontrada.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var pendingDocs = assignment.Documents
                .Where(d => d.Status && d.StatusCode == DocumentStatusCodes.PENDING)
                .ToList();

            if (!pendingDocs.Any())
            {
                TempData["ErrorMessage"] = "No hay reportes pendientes para aprobar.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            foreach (var doc in pendingDocs)
            {
                doc.StatusCode = DocumentStatusCodes.APPROVED;
                doc.ReviewDate = DateTime.Now;
                doc.ReviewComments = "Horas aprobadas por asesor académico.";
                if (teacher != null)
                    doc.ReviewedByTeacherId = teacher.Id;
            }

            // Al aprobar reportes pendientes, se sincronizan las horas aprobadas con el total registrado.
            assignment.ApprovedHours = Math.Max(assignment.ApprovedHours, assignment.TotalHours);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Se aprobaron {pendingDocs.Count} reporte(s) y las horas pendientes.";
            return RedirectToAction(nameof(Evaluaciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarHorasPendientes(int assignmentId, string? comments)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var teacher = await GetCurrentTeacherAsync();
            int? teacherId = teacher?.Id;

            if (!isGlobalView && teacher == null)
            {
                TempData["ErrorMessage"] = "Perfil de docente no encontrado.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] = "Debes capturar comentarios para rechazar reportes.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var assignmentQuery = _context.OperationalStudentAssignments
                .Include(x => x.Documents)
                .Where(x => x.Id == assignmentId && x.Status);

            if (!isGlobalView)
                assignmentQuery = assignmentQuery.Where(x => x.TeacherId == teacherId);

            assignmentQuery = FilterAssignmentsByRoleScope(assignmentQuery);

            var assignment = await assignmentQuery.FirstOrDefaultAsync();
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Asignación no encontrada.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var pendingDocs = assignment.Documents
                .Where(d => d.Status && d.StatusCode == DocumentStatusCodes.PENDING)
                .ToList();

            if (!pendingDocs.Any())
            {
                TempData["ErrorMessage"] = "No hay reportes pendientes para rechazar.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            foreach (var doc in pendingDocs)
            {
                doc.StatusCode = DocumentStatusCodes.REJECTED;
                doc.ReviewDate = DateTime.Now;
                doc.ReviewComments = comments.Trim();
                if (teacher != null)
                    doc.ReviewedByTeacherId = teacher.Id;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Se rechazaron {pendingDocs.Count} reporte(s) con retroalimentación.";
            return RedirectToAction(nameof(Evaluaciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarHorasManual(int assignmentId, decimal approvedHours, string? comments)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var teacher = await GetCurrentTeacherAsync();
            int? teacherId = teacher?.Id;

            if (!isGlobalView && teacher == null)
            {
                TempData["ErrorMessage"] = "Perfil de docente no encontrado.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var assignmentQuery = _context.OperationalStudentAssignments
                .Where(x => x.Id == assignmentId && x.Status);

            if (!isGlobalView)
                assignmentQuery = assignmentQuery.Where(x => x.TeacherId == teacherId);

            assignmentQuery = FilterAssignmentsByRoleScope(assignmentQuery);

            var assignment = await assignmentQuery.FirstOrDefaultAsync();
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Asignación no encontrada.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            if (approvedHours < 0)
            {
                TempData["ErrorMessage"] = "Las horas aprobadas no pueden ser negativas.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            assignment.ApprovedHours = Math.Min(approvedHours, assignment.TotalHours);

            if (!string.IsNullOrWhiteSpace(comments))
            {
                var stamp = $"[AJUSTE MANUAL {DateTime.Now:dd/MM/yyyy HH:mm}] ";
                assignment.EvaluationNotes = string.IsNullOrWhiteSpace(assignment.EvaluationNotes)
                    ? stamp + comments.Trim()
                    : assignment.EvaluationNotes + Environment.NewLine + stamp + comments.Trim();
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Horas aprobadas actualizadas manualmente.";
            return RedirectToAction(nameof(Evaluaciones));
        }

        // GET: /AsesorAcademico/Evaluaciones
        public async Task<IActionResult> Evaluaciones(string? search, string? status, string? module = null)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var roleScopedModule = GetRoleScopedModule();
            ViewData["Title"] = isGlobalView ? "Evaluaciones Académicas Globales" : "Evaluaciones Académicas";

            Teacher? teacher = null;
            int? teacherId = null;
            if (!isGlobalView)
            {
                teacher = await GetCurrentTeacherAsync();
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "No se encontró el perfil de docente/tutor.";
                    return View(new AsesorEvaluacionesPageViewModel());
                }

                teacherId = teacher.Id;
            }

            var assignmentQuery = _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Student).ThenInclude(x => x.Person)
                .Include(x => x.Teacher!).ThenInclude(x => x.Person)
                .Include(x => x.Program)
                .Where(x => x.Status);

            if (!isGlobalView)
                assignmentQuery = assignmentQuery.Where(x => x.TeacherId == teacherId);

            assignmentQuery = FilterAssignmentsByRoleScope(assignmentQuery);

            var requestedModule = NormalizeRequestedModule(module);
            if (!string.IsNullOrWhiteSpace(requestedModule))
                assignmentQuery = assignmentQuery.Where(x => x.Program.Type == requestedModule);

            var assignments = await assignmentQuery
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var reportStats = await _context.OperationalDocuments
                .AsNoTracking()
                .Where(d => d.Status && assignmentIds.Contains(d.AssignmentId))
                .GroupBy(d => d.AssignmentId)
                .Select(g => new
                {
                    AssignmentId = g.Key,
                    Total = g.Count(),
                    Pending = g.Count(x => x.StatusCode == DocumentStatusCodes.PENDING),
                    Approved = g.Count(x => x.StatusCode == DocumentStatusCodes.APPROVED)
                })
                .ToListAsync();

            var reportStatsByAssignment = reportStats.ToDictionary(x => x.AssignmentId, x => x);

            var allRows = assignments.Select(a =>
            {
                var name = a.Student.Person.FullName;
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initials = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}"
                    : name.Length > 0 ? name[0].ToString() : "?";
                reportStatsByAssignment.TryGetValue(a.Id, out var stats);
                return new AsesorEvaluacionRowViewModel
                {
                    AssignmentId = a.Id,
                    StudentName = name,
                    Initials = initials.ToUpper(),
                    Matricula = a.Student.Matricula ?? "N/A",
                    AdvisorName = a.Teacher?.Person?.FullName ?? "Sin asesor",
                    ProgramName = a.Program.Name,
                    ProgramType = a.Program.Type,
                    TotalHours = a.TotalHours,
                    ApprovedHours = a.ApprovedHours,
                    RequiredHours = a.Program.RequiredHours,
                    EvaluationScore = a.EvaluationScore,
                    EvaluationNotes = a.EvaluationNotes,
                    StatusCode = a.StatusCode,
                    TotalReports = stats?.Total ?? 0,
                    PendingReports = stats?.Pending ?? 0,
                    ApprovedReports = stats?.Approved ?? 0,
                };
            }).ToList();

            var evaluated = allRows.Where(r => r.IsEvaluated).ToList();
            var avgScore = evaluated.Any()
                ? evaluated.Average(r => (double)r.EvaluationScore!.Value)
                : (double?)null;

            var vm = new AsesorEvaluacionesPageViewModel
            {
                TotalStudents = allRows.Count,
                ReadyToEvaluate = allRows.Count(r => r.ReadyForEvaluation),
                Evaluated = evaluated.Count,
                AverageScore = avgScore.HasValue ? avgScore.Value.ToString("F1") : "N/A",
                StatusFilter = status ?? "",
                SearchText = search ?? "",
            };

            var filteredRows = allRows;
            if (!string.IsNullOrWhiteSpace(status))
            {
                filteredRows = status switch
                {
                    "EVALUATED" => allRows.Where(r => r.IsEvaluated).ToList(),
                    "READY" => allRows.Where(r => r.ReadyForEvaluation).ToList(),
                    "PENDING" => allRows.Where(r => !r.IsEvaluated && !r.ReadyForEvaluation).ToList(),
                    _ => allRows,
                };
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                filteredRows = filteredRows.Where(r =>
                    r.StudentName.ToLower().Contains(s) ||
                    r.Matricula.ToLower().Contains(s)).ToList();
            }

            vm.Rows = filteredRows;
            ViewBag.ModuleFilter = requestedModule ?? "";
            ViewBag.RoleScopedModule = roleScopedModule ?? "";
            ViewBag.IsGlobalAdvisorView = isGlobalView;
            return View(vm);
        }

        // POST: /AsesorAcademico/GuardarEvaluacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEvaluacion(int assignmentId, decimal score, string? notes)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var teacher = await GetCurrentTeacherAsync();
            int? teacherId = teacher?.Id;
            if (!isGlobalView && teacher == null)
            {
                TempData["ErrorMessage"] = "Perfil de docente no encontrado.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var assignmentQuery = _context.OperationalStudentAssignments
                .Include(x => x.Program)
                .Where(x => x.Id == assignmentId && x.Status);

            if (!isGlobalView)
                assignmentQuery = assignmentQuery.Where(x => x.TeacherId == teacherId);

            assignmentQuery = FilterAssignmentsByRoleScope(assignmentQuery);

            var assignment = await assignmentQuery.FirstOrDefaultAsync();
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Asignación no encontrada.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var requiredHours = assignment.Program?.RequiredHours ?? 0;

            var pendingReports = await _context.OperationalDocuments
                .AsNoTracking()
                .Where(d => d.Status && d.AssignmentId == assignmentId && d.StatusCode == DocumentStatusCodes.PENDING)
                .CountAsync();

            if (pendingReports > 0)
            {
                TempData["ErrorMessage"] = "Hay reportes/bitácoras pendientes por aprobar antes de evaluar al estudiante.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var progressPercent = requiredHours > 0
                ? (assignment.ApprovedHours / requiredHours) * 100m
                : 0m;

            if (!assignment.EvaluationScore.HasValue && progressPercent < 80m)
            {
                TempData["ErrorMessage"] = "El estudiante aún no cumple el avance mínimo (80%) para evaluación inicial.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            assignment.EvaluationScore = Math.Min(10m, Math.Max(0m, score));
            assignment.EvaluationNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            await _context.SaveChangesAsync();

            _logger.LogInformation("AsesorAcademico GuardarEvaluacion TeacherId={T} AssignmentId={A} Score={S}", teacher?.Id, assignmentId, score);
            TempData["SuccessMessage"] = $"Evaluación guardada: {assignment.EvaluationScore:F1}/10.";
            return RedirectToAction(nameof(Evaluaciones));
        }

        // GET: /AsesorAcademico/Index (Dashboard)
        public async Task<IActionResult> Index(string? module = null)
        {
            var isGlobalView = IsCoordinatorGlobalView();
            var roleScopedModule = GetRoleScopedModule();
            ViewData["Title"] = isGlobalView ? "Panel Global de Asesoría Académica" : "Panel de Asesoría Académica";

            Teacher? teacher = null;
            int? teacherId = null;
            if (!isGlobalView)
            {
                teacher = await GetCurrentTeacherAsync();
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "No se encontró el perfil de docente/tutor.";
                    return View(new AsesorDashboardViewModel());
                }

                teacherId = teacher.Id;
            }

            var assignmentQuery = _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Student).ThenInclude(x => x.Person)
                .Include(x => x.Student).ThenInclude(x => x.Career)
                .Include(x => x.Teacher!).ThenInclude(x => x.Person)
                .Include(x => x.Program)
                .Include(x => x.Organization)
                .Where(x => x.Status);

            if (!isGlobalView)
                assignmentQuery = assignmentQuery.Where(x => x.TeacherId == teacherId);

            assignmentQuery = FilterAssignmentsByRoleScope(assignmentQuery);

            var requestedModule = NormalizeRequestedModule(module);
            if (!string.IsNullOrWhiteSpace(requestedModule))
                assignmentQuery = assignmentQuery.Where(x => x.Program.Type == requestedModule);

            var assignments = await assignmentQuery
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var rows = assignments.Select(a =>
            {
                var name = a.Student.Person.FullName;
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initials = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}"
                    : name.Length > 0 ? name[0].ToString() : "?";
                return new AsesorAlumnoRowViewModel
                {
                    AssignmentId = a.Id,
                    StudentName = name,
                    Initials = initials.ToUpper(),
                    Matricula = a.Student.Matricula ?? "N/A",
                    AdvisorName = a.Teacher?.Person?.FullName ?? "Sin asesor",
                    ProgramName = a.Program.Name,
                    ProgramType = a.Program.Type,
                    OrganizationName = a.Organization?.Name ?? "Sin organización",
                    StatusCode = a.StatusCode,
                    ApprovedHours = a.ApprovedHours,
                    RequiredHours = a.Program.RequiredHours,
                    IsEvaluated = a.EvaluationScore.HasValue,
                };
            }).ToList();

            var assignmentIds = assignments.Select(x => x.Id).ToList();

            var evaluated = rows.Where(r => rows.FirstOrDefault(x => x.AssignmentId == r.AssignmentId) != null).ToList();
            var pendingDocs = await _context.OperationalDocuments
                .AsNoTracking()
                .Where(d => d.Status && assignmentIds.Contains(d.AssignmentId) && d.StatusCode == DocumentStatusCodes.PENDING)
                .CountAsync();

            var avgHours = rows.Any() 
                ? (decimal)rows.Average(r => r.ApprovedHours) 
                : 0m;

            var vm = new AsesorDashboardViewModel
            {
                TotalStudents = rows.Count,
                ReadyToEvaluate = rows.Count(r => !rows.Any(x => x.AssignmentId == r.AssignmentId && x.ApprovedHours > 0) && r.ProgressPercent >= 80),
                Evaluated = assignments.Count(a => a.EvaluationScore.HasValue),
                AverageHours = avgHours,
                PendingDocuments = pendingDocs,
                Students = rows,
                RecentStudents = rows.Take(5).ToList()
            };

            ViewBag.ModuleFilter = requestedModule ?? "";
            ViewBag.RoleScopedModule = roleScopedModule ?? "";
            ViewBag.IsGlobalAdvisorView = isGlobalView;
            return View(vm);
        }

        // GET: /AsesorAcademico/BitacoraSeguimiento
        public async Task<IActionResult> BitacoraSeguimiento(int assignmentId)
        {
            ViewData["Title"] = "Bitácora de Seguimiento";

            var isGlobalView = IsCoordinatorGlobalView();
            var teacher = await GetCurrentTeacherAsync();
            int? teacherId = teacher?.Id;
            if (!isGlobalView && teacher == null)
            {
                TempData["ErrorMessage"] = "No se encontró el perfil de docente/tutor.";
                return View(new AsesorSeguimientoViewModel());
            }

            var assignmentQuery = _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Student).ThenInclude(x => x.Person)
                .Include(x => x.Student).ThenInclude(x => x.Career)
                .Include(x => x.Student).ThenInclude(x => x.Group)
                .Include(x => x.Teacher!).ThenInclude(x => x.Person)
                .Include(x => x.Program)
                .Include(x => x.Organization)
                .Where(x => x.Id == assignmentId && x.Status);

            if (!isGlobalView)
                assignmentQuery = assignmentQuery.Where(x => x.TeacherId == teacherId);

            assignmentQuery = FilterAssignmentsByRoleScope(assignmentQuery);

            var assignment = await assignmentQuery.FirstOrDefaultAsync();

            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Asignación no encontrada o no tienes acceso a ella.";
                return RedirectToAction(nameof(AlumnosAsignados));
            }

            var documents = await _context.OperationalDocuments
                .AsNoTracking()
                .Where(x => x.Status && x.AssignmentId == assignmentId)
                .OrderByDescending(x => x.UploadDate)
                .ToListAsync();

            var timeline = documents.Select(d => new AsesorTimelineItemViewModel
            {
                DocumentId = d.Id,
                Title = d.Title,
                DocumentType = d.DocumentType,
                StatusCode = d.StatusCode,
                UploadDate = d.UploadDate,
                Notes = d.Notes,
                FilePath = d.FilePath,
                ReviewComments = d.ReviewComments,
            }).ToList();

            var vm = new AsesorSeguimientoViewModel
            {
                AssignmentId = assignment.Id,
                StudentId = assignment.StudentId,
                StudentName = assignment.Student.Person.FullName,
                Matricula = assignment.Student.Matricula ?? "N/A",
                AdvisorName = assignment.Teacher?.Person?.FullName ?? "Sin asesor",
                CareerCode = assignment.Student.Career?.Name ?? "N/A",
                GroupCode = assignment.Student.Group?.Code ?? "N/A",
                ProgramName = assignment.Program.Name,
                ProgramType = assignment.Program.Type,
                OrganizationName = assignment.Organization?.Name ?? "Sin organización",
                TotalHours = assignment.TotalHours,
                ApprovedHours = assignment.ApprovedHours,
                RequiredHours = assignment.Program.RequiredHours,
                StatusCode = assignment.StatusCode,
                EvaluationScore = assignment.EvaluationScore,
                EvaluationNotes = assignment.EvaluationNotes,
                Timeline = timeline
            };

            ViewBag.IsGlobalAdvisorView = isGlobalView;
            return View(vm);
        }

        private async Task<Teacher?> GetCurrentTeacherAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return null;

            var user = await _context.ManagementUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.management_user_ID == userId && x.management_user_status);

            if (user?.management_user_PersonID == null)
                return null;

            return await _context.TeachersOperational
                .Include(x => x.Person)
                .FirstOrDefaultAsync(x => x.PersonId == user.management_user_PersonID.Value && x.Status);
        }

        private async Task<OperationalDocument?> GetDocumentForAccessScopeAsync(int documentId, int? teacherId, bool isGlobalView)
        {
            var query = _context.OperationalDocuments
                .Include(x => x.Assignment)
                .ThenInclude(x => x.Program)
                .Where(x => x.Id == documentId && x.Status && x.Assignment.Status);

            if (!isGlobalView)
            {
                if (!teacherId.HasValue)
                    return null;

                query = query.Where(x => x.Assignment.TeacherId == teacherId.Value);
            }

            query = FilterDocumentsByRoleScope(query);

            return await query.FirstOrDefaultAsync();
        }

        private IQueryable<OperationalStudentAssignment> FilterAssignmentsByRoleScope(IQueryable<OperationalStudentAssignment> query)
        {
            var requestedModule = GetRoleScopedModule();
            if (string.IsNullOrWhiteSpace(requestedModule))
                return query;

            return query.Where(x => x.Program.Type == requestedModule);
        }

        private IQueryable<OperationalDocument> FilterDocumentsByRoleScope(IQueryable<OperationalDocument> query)
        {
            var requestedModule = GetRoleScopedModule();
            if (string.IsNullOrWhiteSpace(requestedModule))
                return query;

            return query.Where(x => x.Assignment.Program.Type == requestedModule);
        }

        private string? NormalizeRequestedModule(string? module)
        {
            if (!string.IsNullOrWhiteSpace(module))
                return module.Trim().ToUpperInvariant();

            return GetRoleScopedModule();
        }

        private string? GetRoleScopedModule()
        {
            if (HasAnyRole("ADMIN", "COORDINADOR"))
                return null;

            if (HasAnyRole("COORDINADORDUAL", "COORDINADORMODULODUAL", "COORDINADORDUALMODULE"))
                return ProgramTypes.PRACTICAS_PROFESIONALES;

            if (HasAnyRole("COORDINADORSERVICIOSOCIAL", "COORDINADORDESERVICIOSOCIAL"))
                return ProgramTypes.SERVICIO_SOCIAL;

            return null;
        }

        private bool IsCoordinatorGlobalView()
        {
            return HasAnyRole("COORDINADOR", "COORDINADORDUAL", "COORDINADORMODULODUAL", "COORDINADORDUALMODULE", "COORDINADORSERVICIOSOCIAL", "COORDINADORDESERVICIOSOCIAL", "ADMIN");
        }

        private bool HasAnyRole(params string[] canonicalRoles)
        {
            if (canonicalRoles.Length == 0)
                return false;

            var normalizedClaims = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => NormalizeRoleKey(c.Value))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var role in canonicalRoles)
            {
                if (normalizedClaims.Contains(NormalizeRoleKey(role)))
                    return true;
            }

            return false;
        }

        private static string NormalizeRoleKey(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return string.Empty;

            var normalized = role.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    if (ch != ' ' && ch != '_' && ch != '-')
                    {
                        builder.Append(ch);
                    }
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }
    }
}

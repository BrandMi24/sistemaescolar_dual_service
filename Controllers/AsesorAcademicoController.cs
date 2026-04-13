using ControlEscolar.Data;
using ControlEscolar.Models.ManagementOperational;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Models.Operational;
using ControlEscolar.ViewModels.OperationalTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "AsesorAcademico,Asesor,AcademicSupervisor,Tutor,Teacher,Maestro,Admin,Administrator,Master")]
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
        public async Task<IActionResult> AlumnosAsignados(string? search, string? programa)
        {
            ViewData["Title"] = "Mis Estudiantes Asignados";
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "No se encontró el perfil de docente/tutor.";
                ViewBag.SearchText = search ?? "";
                ViewBag.ProgramaFilter = programa ?? "";
                return View(new List<AsesorAlumnoRowViewModel>());
            }

            var query = _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Student).ThenInclude(x => x.Person)
                .Include(x => x.Student).ThenInclude(x => x.Career)
                .Include(x => x.Program)
                .Include(x => x.Organization)
                .Where(x => x.Status && x.TeacherId == teacher.Id);

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
                    ProgramName = a.Program.Name,
                    ProgramType = a.Program.Type,
                    OrganizationName = a.Organization?.Name ?? "Sin organización",
                    StatusCode = a.StatusCode,
                    ApprovedHours = a.ApprovedHours,
                    RequiredHours = a.Program.RequiredHours,
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
            return View(rows);
        }

        // GET: /AsesorAcademico/RevisionDocumentos
        public async Task<IActionResult> RevisionDocumentos(string? search, string? estado)
        {
            ViewData["Title"] = "Validación de Documentación";
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "No se encontró el perfil de docente/tutor.";
                ViewBag.SearchText = "";
                ViewBag.EstadoFilter = "";
                ViewBag.PendingCount = 0;
                return View(new List<AsesorDocumentoRowViewModel>());
            }

            var assignmentIds = await _context.OperationalStudentAssignments
                .AsNoTracking()
                .Where(x => x.Status && x.TeacherId == teacher.Id)
                .Select(x => x.Id)
                .ToListAsync();

            var docQuery = _context.OperationalDocuments
                .AsNoTracking()
                .Include(d => d.Assignment)
                    .ThenInclude(a => a.Student)
                        .ThenInclude(s => s.Person)
                .Include(d => d.Assignment)
                    .ThenInclude(a => a.Student)
                .Where(d => d.Status && assignmentIds.Contains(d.AssignmentId));

            if (!string.IsNullOrWhiteSpace(estado))
                docQuery = docQuery.Where(d => d.StatusCode == estado);

            var docs = await docQuery.OrderByDescending(d => d.UploadDate).ToListAsync();

            var rows = docs.Select(d => new AsesorDocumentoRowViewModel
            {
                DocumentId = d.Id,
                AssignmentId = d.AssignmentId,
                StudentName = d.Assignment.Student.Person.FullName,
                Matricula = d.Assignment.Student.Matricula ?? "N/A",
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
            ViewBag.PendingCount = rows.Count(r => r.StatusCode == DocumentStatusCodes.PENDING);
            return View(rows);
        }

        // POST: /AsesorAcademico/AprobarDocumento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarDocumento(int documentId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "Perfil de docente no encontrado.";
                return RedirectToAction(nameof(RevisionDocumentos));
            }

            var doc = await GetDocumentForTeacherAsync(documentId, teacher.Id);
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
            doc.ReviewedByTeacherId = teacher.Id;
            doc.ReviewDate = DateTime.Now;
            doc.ReviewComments = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("AsesorAcademico AprobarDocumento TeacherId={T} DocId={D}", teacher.Id, documentId);
            TempData["SuccessMessage"] = "Documento aprobado correctamente.";
            return RedirectToAction(nameof(RevisionDocumentos));
        }

        // POST: /AsesorAcademico/RechazarDocumento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarDocumento(int documentId, string? comments)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "Perfil de docente no encontrado.";
                return RedirectToAction(nameof(RevisionDocumentos));
            }

            var doc = await GetDocumentForTeacherAsync(documentId, teacher.Id);
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
            doc.ReviewedByTeacherId = teacher.Id;
            doc.ReviewDate = DateTime.Now;
            doc.ReviewComments = comments.Trim();
            await _context.SaveChangesAsync();

            _logger.LogInformation("AsesorAcademico RechazarDocumento TeacherId={T} DocId={D}", teacher.Id, documentId);
            TempData["SuccessMessage"] = "Retroalimentación enviada al estudiante.";
            return RedirectToAction(nameof(RevisionDocumentos));
        }

        // GET: /AsesorAcademico/Evaluaciones
        public async Task<IActionResult> Evaluaciones(string? search, string? status)
        {
            ViewData["Title"] = "Evaluaciones Académicas";
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "No se encontró el perfil de docente/tutor.";
                return View(new AsesorEvaluacionesPageViewModel());
            }

            var assignments = await _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Student).ThenInclude(x => x.Person)
                .Include(x => x.Program)
                .Where(x => x.Status && x.TeacherId == teacher.Id)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var allRows = assignments.Select(a =>
            {
                var name = a.Student.Person.FullName;
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initials = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}"
                    : name.Length > 0 ? name[0].ToString() : "?";
                return new AsesorEvaluacionRowViewModel
                {
                    AssignmentId = a.Id,
                    StudentName = name,
                    Initials = initials.ToUpper(),
                    Matricula = a.Student.Matricula ?? "N/A",
                    ProgramName = a.Program.Name,
                    ProgramType = a.Program.Type,
                    ApprovedHours = a.ApprovedHours,
                    RequiredHours = a.Program.RequiredHours,
                    EvaluationScore = a.EvaluationScore,
                    EvaluationNotes = a.EvaluationNotes,
                    StatusCode = a.StatusCode,
                };
            }).ToList();

            var evaluated = allRows.Where(r => r.IsEvaluated).ToList();
            var avgScore = evaluated.Any()
                ? evaluated.Average(r => (double)r.EvaluationScore!.Value)
                : (double?)null;

            var vm = new AsesorEvaluacionesPageViewModel
            {
                TotalStudents = allRows.Count,
                ReadyToEvaluate = allRows.Count(r => !r.IsEvaluated && r.ProgressPercent >= 80),
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
                    "READY" => allRows.Where(r => !r.IsEvaluated && r.ProgressPercent >= 80).ToList(),
                    "PENDING" => allRows.Where(r => !r.IsEvaluated && r.ProgressPercent < 80).ToList(),
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
            return View(vm);
        }

        // POST: /AsesorAcademico/GuardarEvaluacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEvaluacion(int assignmentId, decimal score, string? notes)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "Perfil de docente no encontrado.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var assignment = await _context.OperationalStudentAssignments
                .Include(x => x.Program)
                .FirstOrDefaultAsync(x => x.Id == assignmentId && x.Status && x.TeacherId == teacher.Id);
            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Asignación no encontrada.";
                return RedirectToAction(nameof(Evaluaciones));
            }

            var requiredHours = assignment.Program?.RequiredHours ?? 0;

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

            _logger.LogInformation("AsesorAcademico GuardarEvaluacion TeacherId={T} AssignmentId={A} Score={S}", teacher.Id, assignmentId, score);
            TempData["SuccessMessage"] = $"Evaluación guardada: {assignment.EvaluationScore:F1}/10.";
            return RedirectToAction(nameof(Evaluaciones));
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(AlumnosAsignados));
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

        private async Task<OperationalDocument?> GetDocumentForTeacherAsync(int documentId, int teacherId)
        {
            var assignmentIds = await _context.OperationalStudentAssignments
                .Where(x => x.Status && x.TeacherId == teacherId)
                .Select(x => x.Id)
                .ToListAsync();

            return await _context.OperationalDocuments
                .FirstOrDefaultAsync(x => x.Id == documentId && x.Status && assignmentIds.Contains(x.AssignmentId));
        }
    }
}

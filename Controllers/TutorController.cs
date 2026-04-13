using ControlEscolar.Data;
using ControlEscolar.Models;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Services;
using ControlEscolar.ViewModels.OperationalTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Tutor,Teacher,Maestro,AcademicSupervisor,AsesorAcademico,Asesor,Admin,Administrator,Master")]
    public class TutorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TutorController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IOperationalAuditService _auditService;

        // Inyectamos la base de datos
        public TutorController(
            ApplicationDbContext context,
            ILogger<TutorController> logger,
            IWebHostEnvironment env,
            IOperationalAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _env = env;
            _auditService = auditService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Asistencia()
        {
            return View();
        }

        public IActionResult Entrevista()
        {
            return View();
        }

        public async Task<IActionResult> Seguimiento(int? assignmentId = null)
        {
            var vm = new TutorSeguimientoViewModel();
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "No se encontro el perfil de tutor/docente.";
                return View(vm);
            }

            var query = _context.OperationalStudentAssignments
                .AsNoTracking()
                .Include(x => x.Student)
                    .ThenInclude(x => x.Person)
                .Include(x => x.Student)
                    .ThenInclude(x => x.Career)
                .Include(x => x.Student)
                    .ThenInclude(x => x.Group)
                .Include(x => x.Program)
                .Where(x => x.Status && x.TeacherId == teacher.Id);

            var assignment = assignmentId.HasValue
                ? await query.FirstOrDefaultAsync(x => x.Id == assignmentId.Value)
                : await query.OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();

            if (assignment == null)
            {
                return View(vm);
            }

            var docs = await _context.OperationalDocuments
                .AsNoTracking()
                .Where(x => x.Status && x.AssignmentId == assignment.Id)
                .OrderByDescending(x => x.UploadDate)
                .ToListAsync();

            vm.AssignmentId = assignment.Id;
            vm.StudentId = assignment.StudentId;
            vm.StudentName = assignment.Student.Person.FullName;
            vm.Matricula = assignment.Student.Matricula ?? "N/A";
            vm.CareerCode = assignment.Student.Career?.Name ?? "N/A";
            vm.GroupCode = assignment.Student.Group?.Code ?? "N/A";
            vm.ApprovedHours = assignment.ApprovedHours;
            vm.RequiredHours = assignment.Program.RequiredHours;
            vm.StatusCode = assignment.StatusCode;
            vm.TimelineDocuments = docs;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Tutor,Teacher,AcademicSupervisor,Admin,Administrator")]
        public async Task<IActionResult> ApproveDocument(int documentId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "No se encontro el perfil de docente.";
                return RedirectToAction(nameof(Seguimiento));
            }

            var document = await _context.OperationalDocuments.FirstOrDefaultAsync(x => x.Id == documentId && x.Status);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Documento no encontrado.";
                return RedirectToAction(nameof(Seguimiento));
            }

            document.StatusCode = DocumentStatusCodes.APPROVED;
            document.ReviewedByTeacherId = teacher.Id;
            document.ReviewDate = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("TutorAction ApproveDocument TeacherId={TeacherId} DocumentId={DocumentId}", teacher.Id, documentId);
            await _auditService.LogAsync(
                module: "DUAL_SS",
                action: "ApproveDocument",
                entityName: "OperationalDocument",
                entityId: documentId,
                details: $"TeacherId={teacher.Id}");

            TempData["SuccessMessage"] = "Documento aprobado.";
            return RedirectToAction(nameof(Seguimiento));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Tutor,Teacher,AcademicSupervisor,Admin,Administrator")]
        public async Task<IActionResult> RejectDocument(int documentId, string? comments)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                TempData["ErrorMessage"] = "No se encontro el perfil de docente.";
                return RedirectToAction(nameof(Seguimiento));
            }

            var document = await _context.OperationalDocuments.FirstOrDefaultAsync(x => x.Id == documentId && x.Status);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Documento no encontrado.";
                return RedirectToAction(nameof(Seguimiento));
            }

            document.StatusCode = DocumentStatusCodes.REJECTED;
            document.ReviewedByTeacherId = teacher.Id;
            document.ReviewDate = DateTime.Now;
            document.ReviewComments = comments;
            await _context.SaveChangesAsync();

            _logger.LogInformation("TutorAction RejectDocument TeacherId={TeacherId} DocumentId={DocumentId}", teacher.Id, documentId);
            await _auditService.LogAsync(
                module: "DUAL_SS",
                action: "RejectDocument",
                entityName: "OperationalDocument",
                entityId: documentId,
                details: $"TeacherId={teacher.Id};Comments={comments}");

            TempData["SuccessMessage"] = "Documento rechazado.";
            return RedirectToAction(nameof(Seguimiento));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Tutor,Teacher,AcademicSupervisor,Admin,Administrator")]
        public async Task<IActionResult> ApproveHours(int assignmentId, decimal approvedHours)
        {
            var teacher = await GetCurrentTeacherAsync();
            var assignment = await _context.OperationalStudentAssignments
                .Include(x => x.Program)
                .FirstOrDefaultAsync(x => x.Id == assignmentId && x.Status);

            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Asignacion no encontrada.";
                return RedirectToAction(nameof(Seguimiento));
            }

            var maxHours = assignment.Program?.RequiredHours ?? 480;
            assignment.ApprovedHours = Math.Min(maxHours, approvedHours);
            if (assignment.ApprovedHours >= maxHours)
            {
                assignment.StatusCode = SSStatusCodes.COMPLETED;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("TutorAction ApproveHours TeacherId={TeacherId} AssignmentId={AssignmentId} ApprovedHours={ApprovedHours}", teacher?.Id, assignmentId, assignment.ApprovedHours);
            await _auditService.LogAsync(
                module: "DUAL_SS",
                action: "ApproveHours",
                entityName: "OperationalStudentAssignment",
                entityId: assignmentId,
                details: $"TeacherId={teacher?.Id};ApprovedHours={assignment.ApprovedHours}");
            TempData["SuccessMessage"] = "Horas aprobadas correctamente.";
            return RedirectToAction(nameof(Seguimiento));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Tutor,Teacher,AcademicSupervisor,Admin,Administrator")]
        public async Task<IActionResult> FinalApproval(int assignmentId)
        {
            var assignment = await _context.OperationalStudentAssignments
                .Include(x => x.Program)
                .FirstOrDefaultAsync(x => x.Id == assignmentId && x.Status);

            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Asignacion no encontrada.";
                return RedirectToAction(nameof(Seguimiento));
            }

            var requiredHours = assignment.Program?.RequiredHours ?? 480;
            if (assignment.ApprovedHours < requiredHours)
            {
                TempData["ErrorMessage"] = "El alumno aun no cubre las horas requeridas.";
                return RedirectToAction(nameof(Seguimiento));
            }

            assignment.StatusCode = assignment.Program?.Type == ProgramTypes.SERVICIO_SOCIAL
                ? SSStatusCodes.RELEASED
                : DualStatusCodes.FINALIZED;

            await _context.SaveChangesAsync();
            _logger.LogInformation("TutorAction FinalApproval AssignmentId={AssignmentId} FinalStatus={StatusCode}", assignmentId, assignment.StatusCode);
            await _auditService.LogAsync(
                module: "DUAL_SS",
                action: "FinalApproval",
                entityName: "OperationalStudentAssignment",
                entityId: assignmentId,
                details: $"FinalStatus={assignment.StatusCode}");
            TempData["SuccessMessage"] = "Aprobacion final aplicada.";
            return RedirectToAction(nameof(Seguimiento));
        }

        [HttpGet]
        [Authorize(Roles = "Tutor,Teacher,AcademicSupervisor,Admin,Administrator")]
        public async Task<IActionResult> DownloadOperationalDocument(int documentId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return Forbid();
            }

            var document = await _context.OperationalDocuments
                .Include(x => x.Assignment)
                .FirstOrDefaultAsync(x => x.Id == documentId && x.Status);

            if (document == null || string.IsNullOrWhiteSpace(document.FilePath))
            {
                return NotFound();
            }

            if (document.Assignment.TeacherId != teacher.Id)
            {
                return Forbid();
            }

            var relativePath = document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relativePath);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType;
            var fileName = string.IsNullOrWhiteSpace(document.OriginalFileName) ? Path.GetFileName(fullPath) : document.OriginalFileName;

            _logger.LogInformation("TutorAction DownloadDocument TeacherId={TeacherId} DocumentId={DocumentId}", teacher.Id, documentId);
            await _auditService.LogAsync(
                module: "DUAL_SS",
                action: "DownloadDocument",
                entityName: "OperationalDocument",
                entityId: documentId,
                details: $"TeacherId={teacher.Id}");

            return PhysicalFile(fullPath, contentType, fileName);
        }

        // ====================================================
        // AQUÍ CARGAMOS LA BANDEJA DE TRÁMITES PARA EL TUTOR
        // ====================================================
        public IActionResult Tramites(string estatus = "Todos")
        {
            var listado = _context.Set<DetalleSolicitudViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_admin_get_solicitudes'")
                .AsEnumerable()
                .ToList();

            if (estatus != "Todos")
            {
                listado = listado.Where(x => x.Estatus == estatus).ToList();
            }

            ViewBag.EstatusActual = estatus;
            return View(listado);
        }

        private async Task<ControlEscolar.Models.ManagementOperational.Teacher?> GetCurrentTeacherAsync()
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

            return await _context.TeachersOperational
                .Include(x => x.Person)
                .FirstOrDefaultAsync(x => x.PersonId == user.management_user_PersonID.Value && x.Status);
        }
    }
}
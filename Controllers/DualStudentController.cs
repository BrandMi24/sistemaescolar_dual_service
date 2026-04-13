using ControlEscolar.Data;
using ControlEscolar.Models;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Models.Operational;
using ControlEscolar.Services;
using ControlEscolar.ViewModels.DualEducation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ControlEscolar.Controllers;

[Authorize(Roles = "Alumno,Student")]
public class DualStudentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDualEducationService _dualService;
    private readonly IWebHostEnvironment _env;

    public DualStudentController(ApplicationDbContext context, IDualEducationService dualService, IWebHostEnvironment env)
    {
        _context = context;
        _dualService = dualService;
        _env = env;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentInfo(StudentInfoViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        student.Person.Email = vm.InstitutionalEmail ?? student.Person.Email;
        student.Person.Phone = vm.MobilePhone ?? student.Person.Phone;

        if (!string.IsNullOrWhiteSpace(vm.GroupCode) && student.Group != null)
        {
            student.Group.Code = vm.GroupCode;
        }

        if (!string.IsNullOrWhiteSpace(vm.Shift) && student.Group != null)
        {
            student.Group.Shift = vm.Shift;
        }

        var assignment = await _dualService.EnsureAssignmentAsync(student.Id);

        var profileMetadata = ParseMetadata(assignment.EvaluationNotes);
        UpsertMetadata(profileMetadata, "Perfil.Grado", vm.Grade);
        UpsertMetadata(profileMetadata, "Perfil.PeriodoEstadias", vm.InternshipPeriod);
        UpsertMetadata(profileMetadata, "Perfil.AnioEstadias", vm.InternshipYear?.ToString());
        assignment.EvaluationNotes = BuildMetadata(profileMetadata);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Informacion del perfil dual guardada.";
        return RedirectToAction("ModeloDual", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlacementSupport(PlacementSupportViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        var assignment = await _dualService.EnsureAssignmentAsync(student.Id);

        var selectedFromParks = vm.SelectedOrganizationId;
        if (!selectedFromParks.HasValue && vm.SelectedParkIds.Count > 0)
        {
            selectedFromParks = vm.SelectedParkIds.First();
        }

        var organization = await _dualService.UpsertOrganizationAsync(
            ProgramTypes.PRACTICAS_PROFESIONALES,
            selectedFromParks,
            vm.CompanyLegalName,
            vm.CompanyAddress,
            vm.HRContactName,
            vm.HRContactEmail,
            vm.HRContactPhone);

        assignment.OrganizationId = organization.Id;
        assignment.StatusCode = DualStatusCodes.PLACEMENT;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Empresa asociada correctamente al proceso dual.";
        return RedirectToAction("ModeloDual", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Documents(DocumentsViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        var assignment = await _dualService.EnsureAssignmentAsync(student.Id);
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "dual", student.Id.ToString());
        Directory.CreateDirectory(uploadsDir);

        var latestByType = await _context.OperationalDocuments
            .AsNoTracking()
            .Where(x => x.Status
                && x.AssignmentId == assignment.Id
                && (x.DocumentType == "RESUME_SPANISH"
                    || x.DocumentType == "RESUME_ENGLISH"
                    || x.DocumentType == "IMSS_CERTIFICATE"))
            .OrderByDescending(x => x.UploadDate)
            .ToListAsync();

        var latestSpanish = latestByType.FirstOrDefault(x => x.DocumentType == "RESUME_SPANISH");
        var latestEnglish = latestByType.FirstOrDefault(x => x.DocumentType == "RESUME_ENGLISH");
        var latestImss = latestByType.FirstOrDefault(x => x.DocumentType == "IMSS_CERTIFICATE");

        await SaveDocumentIfPresentAsync(vm.ResumeSpanishFile, "RESUME_SPANISH", assignment.Id, student.Id, uploadsDir, latestSpanish);
        await SaveDocumentIfPresentAsync(vm.ResumeEnglishFile, "RESUME_ENGLISH", assignment.Id, student.Id, uploadsDir, latestEnglish);
        await SaveDocumentIfPresentAsync(vm.IMSSCertificateFile, "IMSS_CERTIFICATE", assignment.Id, student.Id, uploadsDir, latestImss);

        assignment.StatusCode = DualStatusCodes.DOCUMENTS;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Documentos dual cargados correctamente.";
        return RedirectToAction("ModeloDual", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PresentationLetter(PresentationLetterViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        var assignment = await _dualService.EnsureAssignmentAsync(student.Id);

        await _dualService.SaveDocumentAsync(new OperationalDocument
        {
            AssignmentId = assignment.Id,
            DocumentType = "PRESENTATION_LETTER",
            Title = "Carta de Presentacion",
            Notes = $"Empresa: {vm.CompanyName}; Destinatario: {vm.RecipientName}; Cargo: {vm.RecipientRole}"
        });

        assignment.StatusCode = DualStatusCodes.LETTER_REQUESTED;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Solicitud de carta de presentacion registrada.";
        return RedirectToAction("ModeloDual", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptanceLetter(IFormFile? acceptanceLetterFile)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        if (acceptanceLetterFile == null || acceptanceLetterFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Debes adjuntar la carta de aceptacion en PDF.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        var assignment = await _dualService.EnsureAssignmentAsync(student.Id);

        var existingAcceptance = await _context.OperationalDocuments
            .AsNoTracking()
            .Where(x => x.Status
                && x.AssignmentId == assignment.Id
                && x.DocumentType == "ACCEPTANCE_LETTER")
            .OrderByDescending(x => x.UploadDate)
            .FirstOrDefaultAsync();

        if (existingAcceptance != null)
        {
            TempData["ErrorMessage"] = "La carta de aceptacion ya fue enviada y no permite nueva carga en este estado.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "dual", student.Id.ToString(), "acceptance");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(acceptanceLetterFile.FileName);
        var fileName = $"ACCEPTANCE_LETTER_{student.Id}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await acceptanceLetterFile.CopyToAsync(stream);
        }

        await _dualService.SaveDocumentAsync(new OperationalDocument
        {
            AssignmentId = assignment.Id,
            DocumentType = "ACCEPTANCE_LETTER",
            Title = "Carta de Aceptacion",
            FilePath = $"/uploads/dual/{student.Id}/acceptance/{fileName}",
            OriginalFileName = acceptanceLetterFile.FileName,
            FileSize = acceptanceLetterFile.Length,
            ContentType = acceptanceLetterFile.ContentType
        });

        assignment.StatusCode = DualStatusCodes.ACCEPTANCE_SUBMITTED;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Carta de aceptacion cargada correctamente.";
        return RedirectToAction("ModeloDual", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBusinessAdvisor(string businessName, string? businessRole, string? businessEmail)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        var assignment = await _dualService.EnsureAssignmentAsync(student.Id);

        var profileMetadata = ParseMetadata(assignment.EvaluationNotes);
        UpsertMetadata(profileMetadata, "Asesor empresarial", businessName);
        UpsertMetadata(profileMetadata, "Cargo", businessRole);
        UpsertMetadata(profileMetadata, "Email", businessEmail);
        assignment.EvaluationNotes = BuildMetadata(profileMetadata);
        assignment.StatusCode = DualStatusCodes.ADVISORS_ASSIGNED;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Asesor empresarial guardado.";
        return RedirectToAction("ModeloDual", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WeeklyReports(WeeklyReportsViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        if (vm.ReportFile == null || vm.ReportFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Debes adjuntar el reporte semanal.";
            return RedirectToAction("ModeloDual", "Alumno");
        }

        var assignment = await _dualService.EnsureAssignmentAsync(student.Id);
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "dual", student.Id.ToString(), "reports");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(vm.ReportFile.FileName);
        var fileName = $"WEEKLY_REPORT_{student.Id}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await vm.ReportFile.CopyToAsync(stream);
        }

        await _dualService.SaveDocumentAsync(new OperationalDocument
        {
            AssignmentId = assignment.Id,
            DocumentType = "WEEKLY_REPORT",
            Title = string.IsNullOrWhiteSpace(vm.ReportTitle) ? $"Reporte semana {vm.WeekNumber}" : vm.ReportTitle,
            Notes = $"Semana: {vm.WeekNumber}; Horas: {vm.HoursWorked}",
            FilePath = $"/uploads/dual/{student.Id}/reports/{fileName}",
            OriginalFileName = vm.ReportFile.FileName,
            FileSize = vm.ReportFile.Length,
            ContentType = vm.ReportFile.ContentType
        });

        assignment.TotalHours += vm.HoursWorked;
        assignment.StatusCode = DualStatusCodes.IN_PROGRESS;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Reporte semanal enviado correctamente.";
        return RedirectToAction("ModeloDual", "Alumno");
    }

    private async Task SaveDocumentIfPresentAsync(IFormFile? file, string docType, int assignmentId, int studentId, string uploadsDir, OperationalDocument? latestDocument)
    {
        if (file == null || file.Length == 0)
        {
            return;
        }

        if (latestDocument != null && !string.Equals(latestDocument.StatusCode, DocumentStatusCodes.REJECTED, StringComparison.OrdinalIgnoreCase))
        {
            // Existing document is pending/approved, so replacement is not allowed.
            return;
        }

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{docType}_{studentId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        await _dualService.SaveDocumentAsync(new OperationalDocument
        {
            AssignmentId = assignmentId,
            DocumentType = docType,
            Title = docType.Replace("_", " "),
            FilePath = $"/uploads/dual/{studentId}/{fileName}",
            OriginalFileName = file.FileName,
            FileSize = file.Length,
            ContentType = file.ContentType
        });
    }

    private async Task<ControlEscolar.Models.ManagementOperational.Student?> GetCurrentStudentAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _context.ManagementUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.management_user_ID == userId.Value && x.management_user_status);

        if (user?.management_user_PersonID == null)
        {
            return null;
        }

        return await _context.StudentsOperational
            .Include(s => s.Person)
            .Include(s => s.Group)
            .Include(s => s.Career)
            .FirstOrDefaultAsync(s => s.PersonId == user.management_user_PersonID.Value && s.Status);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var userId) ? userId : null;
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

    private static void UpsertMetadata(IDictionary<string, string> metadata, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = value.Trim();
    }

    private static string? BuildMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.Count == 0)
        {
            return null;
        }

        return string.Join("; ", metadata.Select(x => $"{x.Key}: {x.Value}"));
    }
}

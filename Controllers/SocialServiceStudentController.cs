using ControlEscolar.Data;
using ControlEscolar.Models;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Models.Operational;
using ControlEscolar.Services;
using ControlEscolar.ViewModels.SocialService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ControlEscolar.Controllers;

[Authorize(Roles = "Alumno,Student")]
public class SocialServiceStudentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ISocialServiceService _socialService;
    private readonly IModuleFlowConfigurationService _moduleFlowConfigurationService;
    private readonly IWebHostEnvironment _env;

    public SocialServiceStudentController(
        ApplicationDbContext context,
        ISocialServiceService socialService,
        IModuleFlowConfigurationService moduleFlowConfigurationService,
        IWebHostEnvironment env)
    {
        _context = context;
        _socialService = socialService;
        _moduleFlowConfigurationService = moduleFlowConfigurationService;
        _env = env;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentInfo(SocialServiceStudentProfileViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var assignment = await _socialService.EnsureAssignmentAsync(student.Id);
        var currentCuatrimestre = await _moduleFlowConfigurationService.ExtractCuatrimestreFromGroupCodeAsync(student.Group?.Code);
        var access = await _moduleFlowConfigurationService.BuildAccessAsync(
            ProgramTypes.SERVICIO_SOCIAL,
            currentCuatrimestre,
            assignment.StatusCode);

        if (!access.CanAccessPortal || !access.IsStepVisible("PASO1"))
        {
            TempData["ErrorMessage"] = "El paso 1 de servicio social no está habilitado para tu cuatrimestre o estatus actual.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        student.Person.Email = vm.InstitutionalEmail ?? student.Person.Email;
        student.Person.Phone = vm.MobilePhone ?? student.Person.Phone;

        var profileMetadata = ParseMetadata(assignment.EvaluationNotes);
        UpsertMetadata(profileMetadata, "Perfil.Grado", currentCuatrimestre?.ToString());
        UpsertMetadata(profileMetadata, "Perfil.PeriodoServicio", vm.ServicePeriod);
        UpsertMetadata(profileMetadata, "Perfil.AnioServicio", vm.ServiceYear?.ToString());
        assignment.EvaluationNotes = BuildMetadata(profileMetadata);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Informacion de servicio social guardada.";
        return RedirectToAction("ServicioSocial", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlacementSupport(SocialServicePlacementViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var assignment = await _socialService.EnsureAssignmentAsync(student.Id);
        var currentCuatrimestre = await _moduleFlowConfigurationService.ExtractCuatrimestreFromGroupCodeAsync(student.Group?.Code);
        var access = await _moduleFlowConfigurationService.BuildAccessAsync(
            ProgramTypes.SERVICIO_SOCIAL,
            currentCuatrimestre,
            assignment.StatusCode);

        if (!access.CanAccessPortal || !access.IsStepVisible("PASO2"))
        {
            TempData["ErrorMessage"] = "El paso 2 de servicio social no está habilitado para tu cuatrimestre o estatus actual.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }
        var selectedOrganizationId = vm.SelectedOrganizationId ?? vm.SelectedInstitutionId ?? vm.SelectedZoneId;

        var organization = await _socialService.UpsertOrganizationAsync(
            selectedOrganizationId,
            vm.InstitutionName,
            vm.Address,
            vm.ContactName,
            vm.Email,
            vm.Phone);

        assignment.OrganizationId = organization.Id;
        assignment.StatusCode = SSStatusCodes.PLACEMENT;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Institucion asociada correctamente al servicio social.";
        return RedirectToAction("ServicioSocial", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PresentationLetter(SocialServicePresentationLetterViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var assignment = await _socialService.EnsureAssignmentAsync(student.Id);
        var currentCuatrimestre = await _moduleFlowConfigurationService.ExtractCuatrimestreFromGroupCodeAsync(student.Group?.Code);
        var access = await _moduleFlowConfigurationService.BuildAccessAsync(
            ProgramTypes.SERVICIO_SOCIAL,
            currentCuatrimestre,
            assignment.StatusCode);

        if (!access.CanAccessPortal || !access.IsStepVisible("PASO3"))
        {
            TempData["ErrorMessage"] = "El paso 3 de servicio social no está habilitado para tu cuatrimestre o estatus actual.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        await _socialService.SaveDocumentAsync(new OperationalDocument
        {
            AssignmentId = assignment.Id,
            DocumentType = "PRESENTATION_LETTER",
            Title = "Carta de Presentacion",
            Notes = $"Institucion: {vm.InstitutionName}; Destinatario: {vm.RecipientName}; Cargo: {vm.RecipientRole}"
        });

        assignment.StatusCode = SSStatusCodes.LETTER_REQUESTED;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Solicitud de carta de presentacion registrada.";
        return RedirectToAction("ServicioSocial", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptanceLetter(SocialServiceAcceptanceLetterViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var assignment = await _socialService.EnsureAssignmentAsync(student.Id);
        var currentCuatrimestre = await _moduleFlowConfigurationService.ExtractCuatrimestreFromGroupCodeAsync(student.Group?.Code);
        var access = await _moduleFlowConfigurationService.BuildAccessAsync(
            ProgramTypes.SERVICIO_SOCIAL,
            currentCuatrimestre,
            assignment.StatusCode);

        if (!access.CanAccessPortal || !access.IsStepVisible("PASO3"))
        {
            TempData["ErrorMessage"] = "El paso 3 de servicio social no está habilitado para tu cuatrimestre o estatus actual.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        if (vm.AcceptanceLetterFile == null || vm.AcceptanceLetterFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Debes adjuntar la carta de aceptacion en PDF.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var existingAcceptance = await _context.OperationalDocuments
            .AsNoTracking()
            .Where(x => x.Status
                && x.AssignmentId == assignment.Id
                && x.DocumentType == "ACCEPTANCE_LETTER")
            .OrderByDescending(x => x.UploadDate)
            .FirstOrDefaultAsync();

        if (existingAcceptance != null && !string.Equals(existingAcceptance.StatusCode, DocumentStatusCodes.REJECTED, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "La carta de aceptación ya fue enviada y solo puede volver a cargarse si el tutor la rechaza.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "social", student.Id.ToString(), "acceptance");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(vm.AcceptanceLetterFile.FileName);
        var fileName = $"ACCEPTANCE_LETTER_{student.Id}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await vm.AcceptanceLetterFile.CopyToAsync(stream);
        }

        await _socialService.SaveDocumentAsync(new OperationalDocument
        {
            AssignmentId = assignment.Id,
            DocumentType = "ACCEPTANCE_LETTER",
            Title = "Carta de Aceptacion",
            FilePath = $"/uploads/social/{student.Id}/acceptance/{fileName}",
            OriginalFileName = vm.AcceptanceLetterFile.FileName,
            FileSize = vm.AcceptanceLetterFile.Length,
            ContentType = vm.AcceptanceLetterFile.ContentType
        });

        assignment.StatusCode = SSStatusCodes.ACCEPTANCE_SUBMITTED;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Carta de aceptacion cargada correctamente.";
        return RedirectToAction("ServicioSocial", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WeeklyReports(SocialServiceWeeklyReportViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var assignment = await _socialService.EnsureAssignmentAsync(student.Id);
        var currentCuatrimestre = await _moduleFlowConfigurationService.ExtractCuatrimestreFromGroupCodeAsync(student.Group?.Code);
        var access = await _moduleFlowConfigurationService.BuildAccessAsync(
            ProgramTypes.SERVICIO_SOCIAL,
            currentCuatrimestre,
            assignment.StatusCode);

        if (!access.CanAccessTracking || !access.IsStepVisible("PASO5"))
        {
            TempData["ErrorMessage"] = $"Los reportes semanales de servicio social se habilitan desde el cuatrimestre {access.TrackingStartCuatrimestre} y según tu estatus actual.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        if (vm.ReportFile == null || vm.ReportFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Debes adjuntar el reporte semanal en PDF.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var existingReportForWeek = await _context.OperationalDocuments
            .AsNoTracking()
            .Where(x => x.Status
                && x.AssignmentId == assignment.Id
                && x.DocumentType == "WEEKLY_REPORT")
            .OrderByDescending(x => x.UploadDate)
            .ToListAsync();

        if (existingReportForWeek.Any(x => GetWeekNumberFromNotes(x.Notes) == vm.WeekNumber
            && !string.Equals(x.StatusCode, DocumentStatusCodes.REJECTED, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["ErrorMessage"] = $"La semana {vm.WeekNumber} ya fue enviada. Solo puedes volver a subirla si el tutor la rechaza.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "social", student.Id.ToString(), "reports");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(vm.ReportFile.FileName);
        var fileName = $"WEEKLY_REPORT_{student.Id}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await vm.ReportFile.CopyToAsync(stream);
        }

        await _socialService.SaveDocumentAsync(new OperationalDocument
        {
            AssignmentId = assignment.Id,
            DocumentType = "WEEKLY_REPORT",
            Title = string.IsNullOrWhiteSpace(vm.ReportTitle) ? $"Reporte semana {vm.WeekNumber}" : vm.ReportTitle,
            Notes = $"Semana: {vm.WeekNumber}; Horas: {vm.HoursWorked}",
            FilePath = $"/uploads/social/{student.Id}/reports/{fileName}",
            OriginalFileName = vm.ReportFile.FileName,
            FileSize = vm.ReportFile.Length,
            ContentType = vm.ReportFile.ContentType
        });

        assignment.TotalHours += vm.HoursWorked;
        assignment.StatusCode = SSStatusCodes.IN_PROGRESS;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Reporte semanal enviado correctamente.";
        return RedirectToAction("ServicioSocial", "Alumno");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoursLog(SocialServiceHoursLogViewModel vm)
    {
        var student = await GetCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "No se encontro el alumno autenticado.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        var assignment = await _socialService.EnsureAssignmentAsync(student.Id);
        var currentCuatrimestre = await _moduleFlowConfigurationService.ExtractCuatrimestreFromGroupCodeAsync(student.Group?.Code);
        var access = await _moduleFlowConfigurationService.BuildAccessAsync(
            ProgramTypes.SERVICIO_SOCIAL,
            currentCuatrimestre,
            assignment.StatusCode);

        if (!access.CanAccessTracking || !access.IsStepVisible("PASO5"))
        {
            TempData["ErrorMessage"] = $"Las bitácoras de servicio social se habilitan desde el cuatrimestre {access.TrackingStartCuatrimestre} y según tu estatus actual.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }
        var existingHoursLog = await _context.OperationalDocuments
            .AsNoTracking()
            .Where(x => x.Status
                && x.AssignmentId == assignment.Id
                && x.DocumentType == "HOURS_LOG")
            .OrderByDescending(x => x.UploadDate)
            .ToListAsync();

        if (existingHoursLog.Any(x => GetLogDateFromNotes(x.Notes) == vm.LogDate.Date
            && !string.Equals(x.StatusCode, DocumentStatusCodes.REJECTED, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["ErrorMessage"] = $"Ya registraste una bitácora para la fecha {vm.LogDate:dd/MM/yyyy}. Solo puede reemplazarse si el tutor la rechaza.";
            return RedirectToAction("ServicioSocial", "Alumno");
        }

        await _socialService.SaveDocumentAsync(new OperationalDocument
        {
            AssignmentId = assignment.Id,
            DocumentType = "HOURS_LOG",
            Title = $"Bitacora {vm.LogDate:yyyy-MM-dd}",
            Notes = $"Fecha: {vm.LogDate:yyyy-MM-dd}; Horas: {vm.HoursWorked}; Actividad: {vm.ActivityDescription}"
        });

        assignment.TotalHours += vm.HoursWorked;
        assignment.StatusCode = SSStatusCodes.IN_PROGRESS;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Bitacora de horas registrada.";
        return RedirectToAction("ServicioSocial", "Alumno");
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

    private static int? GetWeekNumberFromNotes(string? notes)
    {
        var metadata = ParseMetadata(notes);
        return metadata.TryGetValue("Semana", out var value) && int.TryParse(value, out var week) ? week : null;
    }

    private static DateTime? GetLogDateFromNotes(string? notes)
    {
        var metadata = ParseMetadata(notes);
        return metadata.TryGetValue("Fecha", out var value) && DateTime.TryParse(value, out var logDate)
            ? logDate.Date
            : null;
    }
}

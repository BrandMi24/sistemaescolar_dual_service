using ControlEscolar.Data;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace ControlEscolar.Services;

public class DualEducationService : IDualEducationService
{
    private static readonly string[] DualProgramTypes =
    {
        ProgramTypes.PRACTICAS_PROFESIONALES,
        "DUAL"
    };

    private readonly ApplicationDbContext _context;

    public DualEducationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<OperationalProgram?> GetDefaultProgramAsync()
    {
        return _context.OperationalPrograms
            .Where(x => x.Status && x.IsActive && DualProgramTypes.Contains(x.Type))
            .OrderByDescending(x => x.Year ?? 0)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync();
    }

    public Task<OperationalStudentAssignment?> GetActiveAssignmentAsync(int studentId)
    {
        return _context.OperationalStudentAssignments
            .Include(x => x.Program)
            .Include(x => x.Organization)
            .Include(x => x.Documents.Where(d => d.Status))
            .FirstOrDefaultAsync(x => x.StudentId == studentId && x.Status && DualProgramTypes.Contains(x.Program.Type));
    }

    public async Task<OperationalStudentAssignment> EnsureAssignmentAsync(int studentId)
    {
        var existing = await GetActiveAssignmentAsync(studentId);
        if (existing != null)
        {
            return existing;
        }

        var program = await GetDefaultProgramAsync();
        if (program == null)
        {
            program = await CreateDefaultDualProgramAsync();
        }

        var assignment = new OperationalStudentAssignment
        {
            StudentId = studentId,
            ProgramId = program.Id,
            StatusCode = DualStatusCodes.REGISTERED,
            Status = true,
            CreatedDate = DateTime.Now
        };

        _context.OperationalStudentAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    private async Task<OperationalProgram> CreateDefaultDualProgramAsync()
    {
        var now = DateTime.Now;
        var program = new OperationalProgram
        {
            Code = $"DUAL-AUTO-{now:yyyyMMddHHmmss}",
            Name = "Programa Dual (Auto)",
            Type = ProgramTypes.PRACTICAS_PROFESIONALES,
            Year = now.Year,
            Period = now.Month <= 4 ? "ENE-ABR" : now.Month <= 8 ? "MAY-AGO" : "SEP-DIC",
            RequiredHours = 480,
            IsActive = true,
            Status = true
        };

        _context.OperationalPrograms.Add(program);
        await _context.SaveChangesAsync();
        return program;
    }

    public async Task<OperationalOrganization> UpsertOrganizationAsync(string type, int? selectedOrganizationId, string? name, string? address, string? contactName, string? email, string? phone)
    {
        if (selectedOrganizationId.HasValue && selectedOrganizationId.Value > 0)
        {
            var existing = await _context.OperationalOrganizations.FindAsync(selectedOrganizationId.Value);
            if (existing != null)
            {
                return existing;
            }
        }

        var organization = new OperationalOrganization
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Sin nombre" : name.Trim(),
            Type = type,
            Address = address,
            ContactName = contactName,
            Email = email,
            Phone = phone,
            Status = true,
            CreatedDate = DateTime.Now
        };

        _context.OperationalOrganizations.Add(organization);
        await _context.SaveChangesAsync();
        return organization;
    }

    public async Task SaveDocumentAsync(OperationalDocument document)
    {
        document.StatusCode = DocumentStatusCodes.PENDING;
        document.Status = true;
        document.UploadDate = DateTime.Now;
        document.CreatedDate = DateTime.Now;

        _context.OperationalDocuments.Add(document);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ReassignTeacherAsync(int assignmentId, int teacherId)
    {
        var assignment = await _context.OperationalStudentAssignments.FindAsync(assignmentId);
        if (assignment == null)
        {
            return false;
        }

        assignment.TeacherId = teacherId;
        await _context.SaveChangesAsync();
        return true;
    }
}

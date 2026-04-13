using ControlEscolar.Models.Operational;

namespace ControlEscolar.Services;

public interface ISocialServiceService
{
    Task<OperationalProgram?> GetDefaultProgramAsync();
    Task<OperationalStudentAssignment?> GetActiveAssignmentAsync(int studentId);
    Task<OperationalStudentAssignment> EnsureAssignmentAsync(int studentId);
    Task<OperationalOrganization> UpsertOrganizationAsync(int? selectedOrganizationId, string? name, string? address, string? contactName, string? email, string? phone);
    Task SaveDocumentAsync(OperationalDocument document);
    Task<bool> ReassignTeacherAsync(int assignmentId, int teacherId);
}

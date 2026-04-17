using ControlEscolar.Data;
using ControlEscolar.Models.ManagementOperational;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace ControlEscolar.Services;

public class ModuleFlowConfigurationService : IModuleFlowConfigurationService
{
    private readonly ApplicationDbContext _context;

    public ModuleFlowConfigurationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task EnsureInfrastructureAsync()
    {
        // La creación de tablas y datos iniciales se realiza manualmente
        // mediante el script SQL en scripts/db_module_flow_migration.md
        return Task.CompletedTask;
    }

    public async Task<ModuleFlowAccessResult> BuildAccessAsync(string moduleType, int? currentCuatrimestre, string? assignmentStatusCode)
    {
        await EnsureInfrastructureAsync();

        var normalizedModuleType = (moduleType ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedStatus = string.IsNullOrWhiteSpace(assignmentStatusCode)
            ? DualStatusCodes.REGISTERED
            : assignmentStatusCode.Trim().ToUpperInvariant();

        var config = await _context.Set<OperationalModuleFlowConfig>()
            .AsNoTracking()
            .Where(x => x.Status && x.ModuleType.ToUpper() == normalizedModuleType)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
            .FirstOrDefaultAsync();

        if (config == null)
        {
            config = new OperationalModuleFlowConfig
            {
                ModuleType = normalizedModuleType,
                PortalStartCuatrimestre = 10,
                TrackingStartCuatrimestre = 11,
                Status = true,
            };
        }

        var currentCuat = currentCuatrimestre ?? 0;
        var activeCuatrimestres = await _context.Set<CuatrimestreCatalog>()
            .AsNoTracking()
            .Where(x => x.Status && x.IsActive)
            .Select(x => x.Number)
            .ToListAsync();

        var isCurrentCuatActive = currentCuatrimestre.HasValue && activeCuatrimestres.Contains(currentCuatrimestre.Value);
        var canPortal = isCurrentCuatActive && currentCuat >= config.PortalStartCuatrimestre;
        var canTracking = isCurrentCuatActive && currentCuat >= config.TrackingStartCuatrimestre;

        var rules = await _context.Set<OperationalModuleStepRule>()
            .AsNoTracking()
            .Where(x => x.Status && x.ModuleType.ToUpper() == normalizedModuleType)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        if (rules.Count == 0)
        {
            rules = GetDefaultRules().Where(x => x.ModuleType == normalizedModuleType).OrderBy(x => x.SortOrder).ToList();
        }

        var stepAccess = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules)
        {
            var minCuat = rule.MinCuatrimestre ?? config.PortalStartCuatrimestre;
            var cuatrimestreAllowed = isCurrentCuatActive && currentCuat >= minCuat;

            var allowedStatuses = (rule.AllowedStatusesCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToUpperInvariant())
                .ToList();

            var statusAllowed = allowedStatuses.Count == 0 || allowedStatuses.Contains(normalizedStatus);
            stepAccess[rule.StepCode] = cuatrimestreAllowed && statusAllowed;
        }

        stepAccess["PASO1"] = canPortal && (stepAccess.TryGetValue("PASO1", out var paso1) ? paso1 : true);

        return new ModuleFlowAccessResult
        {
            PortalStartCuatrimestre = config.PortalStartCuatrimestre,
            TrackingStartCuatrimestre = config.TrackingStartCuatrimestre,
            CanAccessPortal = canPortal,
            CanAccessTracking = canTracking,
            StepAccess = stepAccess,
        };
    }

    public Task<int?> ExtractCuatrimestreFromGroupCodeAsync(string? groupCode)
    {
        if (string.IsNullOrWhiteSpace(groupCode))
        {
            return Task.FromResult<int?>(null);
        }

        var digits = new string(groupCode.Trim().TakeWhile(char.IsDigit).ToArray());
        return Task.FromResult(int.TryParse(digits, out var cuatrimestre) ? (int?)cuatrimestre : null);
    }

    private static List<OperationalModuleStepRule> GetDefaultRules()
    {
        var dualStatusesFromStep2 = string.Join(",", new[]
        {
            DualStatusCodes.REGISTERED,
            DualStatusCodes.PLACEMENT,
            DualStatusCodes.PROFILE_COMPLETE,
            DualStatusCodes.DOCUMENTS,
            DualStatusCodes.LETTER_REQUESTED,
            DualStatusCodes.ACCEPTANCE_SUBMITTED,
            DualStatusCodes.ADVISORS_ASSIGNED,
            DualStatusCodes.IN_PROGRESS,
            DualStatusCodes.COMPLETED,
            DualStatusCodes.FINALIZED,
        });

        var dualStatusesFromStep3 = string.Join(",", new[]
        {
            DualStatusCodes.PLACEMENT,
            DualStatusCodes.PROFILE_COMPLETE,
            DualStatusCodes.DOCUMENTS,
            DualStatusCodes.LETTER_REQUESTED,
            DualStatusCodes.ACCEPTANCE_SUBMITTED,
            DualStatusCodes.ADVISORS_ASSIGNED,
            DualStatusCodes.IN_PROGRESS,
            DualStatusCodes.COMPLETED,
            DualStatusCodes.FINALIZED,
        });

        var dualStatusesFromStep4 = string.Join(",", new[]
        {
            DualStatusCodes.DOCUMENTS,
            DualStatusCodes.LETTER_REQUESTED,
            DualStatusCodes.ACCEPTANCE_SUBMITTED,
            DualStatusCodes.ADVISORS_ASSIGNED,
            DualStatusCodes.IN_PROGRESS,
            DualStatusCodes.COMPLETED,
            DualStatusCodes.FINALIZED,
        });

        var dualStatusesFromStep5 = string.Join(",", new[]
        {
            DualStatusCodes.ACCEPTANCE_SUBMITTED,
            DualStatusCodes.ADVISORS_ASSIGNED,
            DualStatusCodes.IN_PROGRESS,
            DualStatusCodes.COMPLETED,
            DualStatusCodes.FINALIZED,
        });

        var socialStatusesFromStep2 = string.Join(",", new[]
        {
            SSStatusCodes.REGISTERED,
            SSStatusCodes.PLACEMENT,
            SSStatusCodes.PROFILE_COMPLETE,
            SSStatusCodes.LETTER_REQUESTED,
            SSStatusCodes.ACCEPTANCE_SUBMITTED,
            SSStatusCodes.ADVISOR_ASSIGNED,
            SSStatusCodes.IN_PROGRESS,
            SSStatusCodes.COMPLETED,
            SSStatusCodes.RELEASED,
        });

        var socialStatusesFromStep3 = string.Join(",", new[]
        {
            SSStatusCodes.PLACEMENT,
            SSStatusCodes.PROFILE_COMPLETE,
            SSStatusCodes.LETTER_REQUESTED,
            SSStatusCodes.ACCEPTANCE_SUBMITTED,
            SSStatusCodes.ADVISOR_ASSIGNED,
            SSStatusCodes.IN_PROGRESS,
            SSStatusCodes.COMPLETED,
            SSStatusCodes.RELEASED,
        });

        var socialStatusesFromStep4 = string.Join(",", new[]
        {
            SSStatusCodes.ACCEPTANCE_SUBMITTED,
            SSStatusCodes.ADVISOR_ASSIGNED,
            SSStatusCodes.IN_PROGRESS,
            SSStatusCodes.COMPLETED,
            SSStatusCodes.RELEASED,
        });

        var socialStatusesFromStep5 = string.Join(",", new[]
        {
            SSStatusCodes.ADVISOR_ASSIGNED,
            SSStatusCodes.IN_PROGRESS,
            SSStatusCodes.COMPLETED,
            SSStatusCodes.RELEASED,
        });

        return new List<OperationalModuleStepRule>
        {
            new() { ModuleType = ProgramTypes.PRACTICAS_PROFESIONALES, StepCode = "PASO1", StepName = "Mis Datos", MinCuatrimestre = 10, AllowedStatusesCsv = null, SortOrder = 1, Status = true },
            new() { ModuleType = ProgramTypes.PRACTICAS_PROFESIONALES, StepCode = "PASO2", StepName = "Empresa/Apoyo", MinCuatrimestre = 10, AllowedStatusesCsv = dualStatusesFromStep2, SortOrder = 2, Status = true },
            new() { ModuleType = ProgramTypes.PRACTICAS_PROFESIONALES, StepCode = "PASO3", StepName = "Documentos", MinCuatrimestre = 10, AllowedStatusesCsv = dualStatusesFromStep3, SortOrder = 3, Status = true },
            new() { ModuleType = ProgramTypes.PRACTICAS_PROFESIONALES, StepCode = "PASO4", StepName = "Carta", MinCuatrimestre = 10, AllowedStatusesCsv = dualStatusesFromStep4, SortOrder = 4, Status = true },
            new() { ModuleType = ProgramTypes.PRACTICAS_PROFESIONALES, StepCode = "PASO5", StepName = "Asesores", MinCuatrimestre = 10, AllowedStatusesCsv = dualStatusesFromStep5, SortOrder = 5, Status = true },
            new() { ModuleType = ProgramTypes.PRACTICAS_PROFESIONALES, StepCode = "PASO6", StepName = "Reportes", MinCuatrimestre = 11, AllowedStatusesCsv = dualStatusesFromStep5, SortOrder = 6, Status = true },

            new() { ModuleType = ProgramTypes.SERVICIO_SOCIAL, StepCode = "PASO1", StepName = "Mis Datos", MinCuatrimestre = 10, AllowedStatusesCsv = null, SortOrder = 1, Status = true },
            new() { ModuleType = ProgramTypes.SERVICIO_SOCIAL, StepCode = "PASO2", StepName = "Institución", MinCuatrimestre = 10, AllowedStatusesCsv = socialStatusesFromStep2, SortOrder = 2, Status = true },
            new() { ModuleType = ProgramTypes.SERVICIO_SOCIAL, StepCode = "PASO3", StepName = "Carta", MinCuatrimestre = 10, AllowedStatusesCsv = socialStatusesFromStep3, SortOrder = 3, Status = true },
            new() { ModuleType = ProgramTypes.SERVICIO_SOCIAL, StepCode = "PASO4", StepName = "Asesor", MinCuatrimestre = 10, AllowedStatusesCsv = socialStatusesFromStep4, SortOrder = 4, Status = true },
            new() { ModuleType = ProgramTypes.SERVICIO_SOCIAL, StepCode = "PASO5", StepName = "Bitácoras/Reportes", MinCuatrimestre = 11, AllowedStatusesCsv = socialStatusesFromStep5, SortOrder = 5, Status = true },
            new() { ModuleType = ProgramTypes.SERVICIO_SOCIAL, StepCode = "PASO6", StepName = "Evaluación", MinCuatrimestre = 11, AllowedStatusesCsv = socialStatusesFromStep5, SortOrder = 6, Status = true },
        };
    }
}

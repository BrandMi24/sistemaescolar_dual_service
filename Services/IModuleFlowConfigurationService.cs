using ControlEscolar.Models.Operational;

namespace ControlEscolar.Services;

public interface IModuleFlowConfigurationService
{
    Task EnsureInfrastructureAsync();
    Task<ModuleFlowAccessResult> BuildAccessAsync(string moduleType, int? currentCuatrimestre, string? assignmentStatusCode);
    Task<int?> ExtractCuatrimestreFromGroupCodeAsync(string? groupCode);
}

public class ModuleFlowAccessResult
{
    public int PortalStartCuatrimestre { get; set; }
    public int TrackingStartCuatrimestre { get; set; }
    public bool CanAccessPortal { get; set; }
    public bool CanAccessTracking { get; set; }
    public Dictionary<string, bool> StepAccess { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsStepVisible(string stepCode)
    {
        return StepAccess.TryGetValue(stepCode, out var allowed) && allowed;
    }
}

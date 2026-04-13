namespace ControlEscolar.Services;

public interface IOperationalAuditService
{
    Task LogAsync(string module, string action, string entityName, int? entityId = null, string? details = null);
}
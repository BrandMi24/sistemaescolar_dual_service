namespace ControlEscolar.Services;

public class OperationalAuditService : IOperationalAuditService
{
    private readonly ILogger<OperationalAuditService> _logger;

    public OperationalAuditService(
        ILogger<OperationalAuditService> logger)
    {
        _logger = logger;
    }

    public async Task LogAsync(string module, string action, string entityName, int? entityId = null, string? details = null)
    {
        _logger.LogDebug(
            "Auditoria deshabilitada. Module={Module} Action={Action} Entity={Entity} EntityId={EntityId}",
            module,
            action,
            entityName,
            entityId);

        await Task.CompletedTask;
    }
}
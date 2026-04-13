using ControlEscolar.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ControlEscolar.Services;

public class OperationalAuditService : IOperationalAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OperationalAuditService> _logger;

    public OperationalAuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OperationalAuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(string module, string action, string entityName, int? entityId = null, string? details = null)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        int? parsedUserId = int.TryParse(userId, out var tmpUserId) ? tmpUserId : null;
        var username = user?.Identity?.Name;
        var role = user?.FindFirst(ClaimTypes.Role)?.Value;

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO dbo.operational_audit_table
                    (
                        operational_audit_Module,
                        operational_audit_Action,
                        operational_audit_Entity,
                        operational_audit_EntityID,
                        operational_audit_UserID,
                        operational_audit_Username,
                        operational_audit_Role,
                        operational_audit_Details,
                        operational_audit_CreatedDate,
                        operational_audit_status
                    )
                    VALUES
                    (
                        @Module,
                        @Action,
                        @Entity,
                        @EntityId,
                        @UserId,
                        @Username,
                        @Role,
                        @Details,
                        GETDATE(),
                        1
                    )",
                new SqlParameter("@Module", (object?)module ?? DBNull.Value),
                new SqlParameter("@Action", (object?)action ?? DBNull.Value),
                new SqlParameter("@Entity", (object?)entityName ?? DBNull.Value),
                new SqlParameter("@EntityId", (object?)entityId ?? DBNull.Value),
                new SqlParameter("@UserId", (object?)parsedUserId ?? DBNull.Value),
                new SqlParameter("@Username", (object?)username ?? DBNull.Value),
                new SqlParameter("@Role", (object?)role ?? DBNull.Value),
                new SqlParameter("@Details", (object?)details ?? DBNull.Value));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo guardar auditoria operativa. Module={Module} Action={Action} Entity={Entity} EntityId={EntityId}",
                module,
                action,
                entityName,
                entityId);
        }
    }
}
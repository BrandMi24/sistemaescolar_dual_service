using ControlEscolar.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlEscolar.Services;

public class OperationalAuditBootstrapService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OperationalAuditBootstrapService> _logger;

    public OperationalAuditBootstrapService(
        ApplicationDbContext context,
        ILogger<OperationalAuditBootstrapService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        const string sql = @"
IF OBJECT_ID(N'dbo.operational_audit_table', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.operational_audit_table
    (
        operational_audit_ID INT IDENTITY(1,1) NOT NULL,
        operational_audit_Module NVARCHAR(50) NOT NULL,
        operational_audit_Action NVARCHAR(80) NOT NULL,
        operational_audit_Entity NVARCHAR(80) NOT NULL,
        operational_audit_EntityID INT NULL,
        operational_audit_UserID INT NULL,
        operational_audit_Username NVARCHAR(100) NULL,
        operational_audit_Role NVARCHAR(80) NULL,
        operational_audit_Details NVARCHAR(500) NULL,
        operational_audit_CreatedDate DATETIME NOT NULL CONSTRAINT DF_operational_audit_CreatedDate DEFAULT (GETDATE()),
        operational_audit_status BIT NOT NULL CONSTRAINT DF_operational_audit_status DEFAULT ((1)),
        CONSTRAINT PK_operational_audit PRIMARY KEY CLUSTERED (operational_audit_ID ASC)
    );

    CREATE INDEX IX_operational_audit_createdDate
        ON dbo.operational_audit_table (operational_audit_CreatedDate DESC);
END";

        try
        {
            await _context.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo inicializar operational_audit_table");
        }
    }
}
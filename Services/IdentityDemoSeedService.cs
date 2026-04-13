using ControlEscolar.Data;
using ControlEscolar.Models.ManagementOperational;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ControlEscolar.Services;

public class IdentityDemoSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IdentityDemoSeedService> _logger;

    public IdentityDemoSeedService(ApplicationDbContext context, ILogger<IdentityDemoSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var roleIds = await EnsureRolesAsync();
        var defaultPasswordHash = HashPassword("Temp1234!");

        var teachers = await _context.TeachersOperational
            .Include(x => x.Person)
            .Where(x => x.Status)
            .OrderBy(x => x.Id)
            .Take(4)
            .ToListAsync();

        var students = await _context.StudentsOperational
            .Include(x => x.Person)
            .Where(x => x.Status)
            .OrderBy(x => x.Id)
            .Take(2)
            .ToListAsync();

        await EnsureUserWithRolesAsync(
            username: "admin.demo",
            email: "admin.demo@demo.local",
            firstName: "Admin",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: defaultPasswordHash,
            roleIds,
            "Admin",
            "Administrator");

        await EnsureUserWithRolesAsync(
            username: "master.demo",
            email: "master.demo@demo.local",
            firstName: "Master",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: defaultPasswordHash,
            roleIds,
            "Master",
            "Admin",
            "Administrator");

        await EnsureUserWithRolesAsync(
            username: "director.demo",
            email: "director.demo@demo.local",
            firstName: "Director",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: defaultPasswordHash,
            roleIds,
            "Director");

        await EnsureUserWithRolesAsync(
            username: "coordinador.demo",
            email: "coordinador.demo@demo.local",
            firstName: "Coordinador",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: defaultPasswordHash,
            roleIds,
            "Coordinador");

        await EnsureUserWithRolesAsync(
            username: "preins.demo",
            email: "preins.demo@demo.local",
            firstName: "Preinscripciones",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: defaultPasswordHash,
            roleIds,
            "Preinscripciones",
            "Admisiones",
            "Administrativo");

        await EnsureUserWithRolesAsync(
            username: "enfermeria.demo",
            email: "enfermeria.demo@demo.local",
            firstName: "Enfermeria",
            lastNamePaternal: "Temporal",
            lastNameMaternal: "Sistema",
            passwordHash: defaultPasswordHash,
            roleIds,
            "Enfermeria",
            "Administrativo");

        if (teachers.Count > 0)
        {
            await EnsureUserForExistingPersonWithRolesAsync(
                teachers[0].PersonId,
                "maestro.demo",
                teachers[0].Person.Email ?? "maestro.demo@demo.local",
                defaultPasswordHash,
                roleIds,
                "Maestro",
                "Teacher");
        }

        if (teachers.Count > 1)
        {
            await EnsureUserForExistingPersonWithRolesAsync(
                teachers[1].PersonId,
                "tutor.demo",
                teachers[1].Person.Email ?? "tutor.demo@demo.local",
                defaultPasswordHash,
                roleIds,
                "Tutor",
                "Teacher");
        }

        if (teachers.Count > 2)
        {
            await EnsureUserForExistingPersonWithRolesAsync(
                teachers[2].PersonId,
                "asesor.demo",
                teachers[2].Person.Email ?? "asesor.demo@demo.local",
                defaultPasswordHash,
                roleIds,
                "Asesor",
                "AsesorAcademico",
                "AcademicSupervisor");
        }

        if (teachers.Count > 3)
        {
            await EnsureUserForExistingPersonWithRolesAsync(
                teachers[3].PersonId,
                "docente.multi.demo",
                teachers[3].Person.Email ?? "docente.multi.demo@demo.local",
                defaultPasswordHash,
                roleIds,
                "Maestro",
                "Tutor",
                "AsesorAcademico");
        }

        if (students.Count > 0)
        {
            await EnsureUserForExistingPersonWithRolesAsync(
                students[0].PersonId,
                "alumno.demo",
                students[0].Person.Email ?? "alumno.demo@demo.local",
                defaultPasswordHash,
                roleIds,
                "Alumno",
                "Student");
        }

        _logger.LogInformation(
            "Identity demo seeder completado. Usuarios demo: admin.demo, master.demo, director.demo, coordinador.demo, preins.demo, enfermeria.demo, maestro.demo, tutor.demo, asesor.demo, docente.multi.demo, alumno.demo. Password=Temp1234!");
    }

    private async Task<Dictionary<string, int>> EnsureRolesAsync()
    {
        var roles = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Admin"] = "Acceso administrativo principal",
            ["Administrator"] = "Alias administrativo compatible",
            ["Master"] = "Superusuario del sistema",
            ["Director"] = "Consulta directiva / dashboard ejecutivo",
            ["Coordinador"] = "Coordinacion academica u operativa",
            ["ServiceLearningCoordinator"] = "Alias de coordinacion operativa",
            ["Maestro"] = "Docente del sistema",
            ["Teacher"] = "Alias compatible para docente",
            ["Tutor"] = "Tutor academico",
            ["Asesor"] = "Asesor academico",
            ["AsesorAcademico"] = "Alias principal de asesor academico",
            ["AcademicSupervisor"] = "Alias compatible de asesor academico",
            ["Alumno"] = "Estudiante del sistema",
            ["Student"] = "Alias compatible para estudiante",
            ["Admisiones"] = "Operador de admisiones",
            ["Preinscripciones"] = "Operador de preinscripciones",
            ["Enfermeria"] = "Personal de enfermeria",
            ["Administrativo"] = "Rol administrativo general"
        };

        var roleIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            roleIds[role.Key] = await GetOrCreateRoleAsync(role.Key, role.Value);
        }

        return roleIds;
    }

    private async Task EnsureUserWithRolesAsync(
        string username,
        string email,
        string firstName,
        string lastNamePaternal,
        string? lastNameMaternal,
        string passwordHash,
        IReadOnlyDictionary<string, int> roleIds,
        params string[] roles)
    {
        var personId = await GetOrCreateManagementPersonAsync(email, firstName, lastNamePaternal, lastNameMaternal);
        await EnsureUserForPersonWithRolesAsync(personId, username, email, passwordHash, roleIds, roles);
    }

    private async Task EnsureUserForExistingPersonWithRolesAsync(
        int personId,
        string username,
        string email,
        string passwordHash,
        IReadOnlyDictionary<string, int> roleIds,
        params string[] roles)
    {
        await EnsureUserForPersonWithRolesAsync(personId, username, email, passwordHash, roleIds, roles);
    }

    private async Task EnsureUserForPersonWithRolesAsync(
        int personId,
        string username,
        string email,
        string passwordHash,
        IReadOnlyDictionary<string, int> roleIds,
        params string[] roles)
    {
        if (roles.Length == 0)
            throw new InvalidOperationException($"El usuario '{username}' debe tener al menos un rol.");

        var primaryRole = roles[0];
        var userId = await GetOrCreateUserAsync(personId, username, email, passwordHash, roleIds[primaryRole]);

        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await EnsureUserRoleAsync(userId, roleIds[role]);
        }
    }

    private async Task<int> GetOrCreateRoleAsync(string roleName, string? description)
    {
        var existingId = await GetScalarIntAsync(
            @"SELECT TOP 1 management_role_ID
              FROM dbo.management_role_table
              WHERE management_role_Name = @RoleName;",
            new SqlParameter("@RoleName", roleName));

        if (existingId.HasValue)
        {
            await ExecuteNonQueryAsync(
                @"UPDATE dbo.management_role_table
                  SET management_role_status = 1,
                      management_role_Description = COALESCE(@Description, management_role_Description)
                  WHERE management_role_ID = @RoleId;",
                new SqlParameter("@Description", (object?)description ?? DBNull.Value),
                new SqlParameter("@RoleId", existingId.Value));

            return existingId.Value;
        }

        var newRoleId = await GetScalarIntAsync(
            @"INSERT INTO dbo.management_role_table
                (management_role_Name, management_role_Description, management_role_status, management_role_createdDate)
              OUTPUT INSERTED.management_role_ID
              VALUES (@RoleName, @Description, 1, GETDATE());",
            new SqlParameter("@RoleName", roleName),
            new SqlParameter("@Description", (object?)description ?? DBNull.Value));

        if (!newRoleId.HasValue)
            throw new InvalidOperationException($"No se pudo crear el rol '{roleName}'.");

        return newRoleId.Value;
    }

    private async Task<int> GetOrCreateManagementPersonAsync(string email, string firstName, string lastNamePaternal, string? lastNameMaternal)
    {
        var existingPersonId = await GetScalarIntAsync(
            @"SELECT TOP 1 management_person_ID
              FROM dbo.management_person_table
              WHERE management_person_Email = @Email
              ORDER BY management_person_ID;",
            new SqlParameter("@Email", email));

        if (existingPersonId.HasValue)
        {
            await ExecuteNonQueryAsync(
                @"UPDATE dbo.management_person_table
                  SET management_person_FirstName = @FirstName,
                      management_person_LastNamePaternal = @LastNamePaternal,
                      management_person_LastNameMaternal = @LastNameMaternal,
                      management_person_Email = @Email,
                      management_person_status = 1
                  WHERE management_person_ID = @PersonId;",
                new SqlParameter("@FirstName", firstName),
                new SqlParameter("@LastNamePaternal", lastNamePaternal),
                new SqlParameter("@LastNameMaternal", (object?)lastNameMaternal ?? DBNull.Value),
                new SqlParameter("@Email", email),
                new SqlParameter("@PersonId", existingPersonId.Value));

            return existingPersonId.Value;
        }

        var newPersonId = await GetScalarIntAsync(
            @"INSERT INTO dbo.management_person_table
                (management_person_FirstName, management_person_LastNamePaternal, management_person_LastNameMaternal, management_person_Email, management_person_status, management_person_createdDate)
              OUTPUT INSERTED.management_person_ID
              VALUES (@FirstName, @LastNamePaternal, @LastNameMaternal, @Email, 1, GETDATE());",
            new SqlParameter("@FirstName", firstName),
            new SqlParameter("@LastNamePaternal", lastNamePaternal),
            new SqlParameter("@LastNameMaternal", (object?)lastNameMaternal ?? DBNull.Value),
            new SqlParameter("@Email", email));

        if (!newPersonId.HasValue)
            throw new InvalidOperationException($"No se pudo crear la persona para '{email}'.");

        return newPersonId.Value;
    }

    private async Task<int> GetOrCreateUserAsync(int personId, string username, string email, string passwordHash, int primaryRoleId)
    {
        var existingUserId = await GetScalarIntAsync(
            @"SELECT TOP 1 management_user_ID
              FROM dbo.management_user_table
              WHERE management_user_Username = @Username;",
            new SqlParameter("@Username", username));

        if (existingUserId.HasValue)
        {
            await ExecuteNonQueryAsync(
                @"UPDATE dbo.management_user_table
                  SET management_user_PersonID = @PersonId,
                      management_user_Email = @Email,
                      management_user_PasswordHash = @PasswordHash,
                      management_user_RoleID = @PrimaryRoleId,
                      management_user_IsLocked = 0,
                      management_user_LockReason = NULL,
                      management_user_status = 1
                  WHERE management_user_ID = @UserId;",
                new SqlParameter("@PersonId", personId),
                new SqlParameter("@Email", email),
                new SqlParameter("@PasswordHash", passwordHash),
                new SqlParameter("@PrimaryRoleId", primaryRoleId),
                new SqlParameter("@UserId", existingUserId.Value));

            return existingUserId.Value;
        }

        var newUserId = await GetScalarIntAsync(
            @"INSERT INTO dbo.management_user_table
                (management_user_PersonID, management_user_Username, management_user_Email, management_user_PasswordHash, management_user_IsLocked, management_user_status, management_user_createdDate, management_user_RoleID)
              OUTPUT INSERTED.management_user_ID
              VALUES (@PersonId, @Username, @Email, @PasswordHash, 0, 1, GETDATE(), @PrimaryRoleId);",
            new SqlParameter("@PersonId", personId),
            new SqlParameter("@Username", username),
            new SqlParameter("@Email", email),
            new SqlParameter("@PasswordHash", passwordHash),
            new SqlParameter("@PrimaryRoleId", primaryRoleId));

        if (!newUserId.HasValue)
            throw new InvalidOperationException($"No se pudo crear el usuario '{username}'.");

        return newUserId.Value;
    }

    private async Task EnsureUserRoleAsync(int userId, int roleId)
    {
        var userRoleId = await GetScalarIntAsync(
            @"SELECT TOP 1 management_userrole_ID
              FROM dbo.management_userrole_table
              WHERE management_userrole_UserID = @UserId
                AND management_userrole_RoleID = @RoleId;",
            new SqlParameter("@UserId", userId),
            new SqlParameter("@RoleId", roleId));

        if (userRoleId.HasValue)
        {
            await ExecuteNonQueryAsync(
                @"UPDATE dbo.management_userrole_table
                  SET management_userrole_status = 1
                  WHERE management_userrole_ID = @UserRoleId;",
                new SqlParameter("@UserRoleId", userRoleId.Value));
            return;
        }

        await ExecuteNonQueryAsync(
            @"INSERT INTO dbo.management_userrole_table
                (management_userrole_UserID, management_userrole_RoleID, management_userrole_status, management_userrole_createdDate)
              VALUES (@UserId, @RoleId, 1, GETDATE());",
            new SqlParameter("@UserId", userId),
            new SqlParameter("@RoleId", roleId));
    }

    private async Task<int?> GetScalarIntAsync(string sql, params SqlParameter[] parameters)
    {
        var conn = _context.Database.GetDbConnection();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;

        foreach (var parameter in parameters)
        {
            cmd.Parameters.Add(parameter);
        }

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        try
        {
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToInt32(result);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private async Task ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
    {
        var conn = _context.Database.GetDbConnection();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;

        foreach (var parameter in parameters)
        {
            cmd.Parameters.Add(parameter);
        }

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
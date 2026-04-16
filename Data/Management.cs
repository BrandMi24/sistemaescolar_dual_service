using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ControlEscolar.Models;

namespace ControlEscolar.Data
{
    public class Management
    {
        private const string StudentDashboardQuery = @"
            SELECT
                s.management_student_ID,
                s.management_student_status,
                s.management_student_StatusCode,
                s.management_student_IsFolio,
                s.management_student_EnrollmentFolio,
                s.management_student_Matricula,
                s.management_student_createdDate,
                p.management_person_ID,
                p.management_person_FirstName,
                p.management_person_LastNamePaternal,
                p.management_person_LastNameMaternal,
                p.management_person_CURP,
                p.management_person_Email,
                p.management_person_Phone,
                c.management_career_ID,
                c.management_career_Code,
                c.management_career_Name,
                g.management_group_ID,
                g.management_group_Code,
                g.management_group_Name,
                g.management_group_Shift,
                TRY_CAST(LEFT(g.management_group_Code, 1) AS INT) AS Grado
            FROM dbo.management_student_table s
            INNER JOIN dbo.management_person_table p
                ON p.management_person_ID = s.management_student_PersonID
            LEFT JOIN dbo.management_career_table c
                ON c.management_career_ID = s.management_student_CareerID
            LEFT JOIN dbo.management_group_table g
                ON g.management_group_ID = s.management_student_GroupID
            WHERE (@ID IS NULL OR s.management_student_ID = @ID)
              AND (@Status IS NULL OR s.management_student_status = @Status)
              AND (@StudentCareerID IS NULL OR s.management_student_CareerID = @StudentCareerID)
              AND (@StudentGroupID IS NULL OR s.management_student_GroupID = @StudentGroupID)
              AND (@Student_IsFolio IS NULL OR s.management_student_IsFolio = @Student_IsFolio)
            ORDER BY s.management_student_ID DESC;";

        private const string UserDashboardQuery = @"
            ;WITH RolesAgg AS
            (
                SELECT
                    ur.management_userrole_UserID AS UserID,
                    STRING_AGG(r.management_role_Name, ', ') WITHIN GROUP (ORDER BY r.management_role_Name) AS Roles
                FROM dbo.management_userrole_table ur
                INNER JOIN dbo.management_role_table r
                    ON r.management_role_ID = ur.management_userrole_RoleID
                WHERE ur.management_userrole_status = 1
                  AND r.management_role_status = 1
                GROUP BY ur.management_userrole_UserID
            )
            SELECT
                u.management_user_ID,
                u.management_user_status,
                u.management_user_Username,
                u.management_user_Email,
                u.management_user_IsLocked,
                u.management_user_LockReason,
                u.management_user_LastLoginDate,
                u.management_user_createdDate,
                p.management_person_ID,
                p.management_person_status,
                p.management_person_FirstName,
                p.management_person_LastNamePaternal,
                p.management_person_LastNameMaternal,
                (p.management_person_FirstName + ' ' +
                     p.management_person_LastNamePaternal + ' ' +
                     ISNULL(p.management_person_LastNameMaternal, '')) AS FullName,
                p.management_person_CURP,
                p.management_person_Email AS person_email,
                p.management_person_Phone,
                ra.Roles,
                s.management_student_ID AS student_ID,
                s.management_student_status AS student_status,
                s.management_student_StatusCode AS student_statuscode,
                s.management_student_IsFolio,
                s.management_student_EnrollmentFolio,
                s.management_student_Matricula,
                c.management_career_Name AS student_career,
                g.management_group_Code AS student_group,
                g.management_group_Name,
                TRY_CAST(LEFT(g.management_group_Code, 1) AS INT) AS student_grado,
                t.management_teacher_ID AS teacher_ID,
                t.management_teacher_status AS teacher_status,
                t.management_teacher_EmployeeNumber,
                t.management_teacher_StatusCode AS teacher_statuscode
            FROM dbo.management_person_table p
            LEFT JOIN dbo.management_user_table u
                ON p.management_person_ID = u.management_user_PersonID
            LEFT JOIN RolesAgg ra
                ON ra.UserID = u.management_user_ID
            LEFT JOIN dbo.management_student_table s
                ON s.management_student_PersonID = p.management_person_ID
            LEFT JOIN dbo.management_career_table c
                ON c.management_career_ID = s.management_student_CareerID
            LEFT JOIN dbo.management_group_table g
                ON g.management_group_ID = s.management_student_GroupID
            LEFT JOIN dbo.management_teacher_table t
                ON t.management_teacher_PersonID = p.management_person_ID
            WHERE (@ID IS NULL OR u.management_user_ID = @ID OR t.management_teacher_ID = @ID OR s.management_student_ID = @ID)
              AND (
                    @Status IS NULL
                    OR (u.management_user_ID IS NOT NULL AND ISNULL(u.management_user_status, 0) = @Status)
                    OR (s.management_student_ID IS NOT NULL AND ISNULL(s.management_student_status, 0) = @Status)
                    OR (t.management_teacher_ID IS NOT NULL AND ISNULL(t.management_teacher_status, 0) = @Status)
                  )
              AND (@UserPersonID IS NULL OR u.management_user_PersonID = @UserPersonID)
              AND (@Username IS NULL OR u.management_user_Username = @Username)
              AND (@UserEmail IS NULL OR u.management_user_Email = @UserEmail)
            ORDER BY ISNULL(u.management_user_ID, 0) DESC,
                     ISNULL(s.management_student_ID, 0) DESC,
                     ISNULL(t.management_teacher_ID, 0) DESC;";

        private const string GroupDashboardQuery = @"
            SELECT
                g.management_group_ID,
                g.management_group_CareerID,
                g.management_group_Code,
                g.management_group_Name,
                g.management_group_Shift,
                g.management_group_status,
                g.management_group_createdDate,
                c.management_career_Name
            FROM dbo.management_group_table g
            LEFT JOIN dbo.management_career_table c
                ON c.management_career_ID = g.management_group_CareerID
            WHERE (@ID IS NULL OR g.management_group_ID = @ID)
              AND (@Status IS NULL OR g.management_group_status = @Status)
              AND (@GroupCareerID IS NULL OR g.management_group_CareerID = @GroupCareerID)
              AND (@GroupCode IS NULL OR g.management_group_Code = @GroupCode)
            ORDER BY g.management_group_ID DESC;";

        private const string BitacoraQuery = @"
            SELECT
                u.management_user_createdDate AS Fecha,
                u.management_user_Username AS Usuario,
                (p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal + ' ' + ISNULL(p.management_person_LastNameMaternal,'')) AS NombreCompleto,
                CASE
                    WHEN u.management_user_status = 1 THEN 'ALTA'
                    ELSE 'BAJA'
                END AS Accion
            FROM dbo.management_user_table u
            INNER JOIN dbo.management_person_table p
                ON p.management_person_ID = u.management_user_PersonID

            UNION ALL

            SELECT
                s.management_student_createdDate,
                'SISTEMA',
                (p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal),
                CASE
                    WHEN s.management_student_status = 1 THEN 'ALTA'
                    ELSE 'BAJA'
                END
            FROM dbo.management_student_table s
            INNER JOIN dbo.management_person_table p
                ON p.management_person_ID = s.management_student_PersonID

            UNION ALL

            SELECT
                t.management_teacher_createdDate,
                ISNULL(u.management_user_Username, 'SISTEMA'),
                (p.management_person_FirstName + ' ' + p.management_person_LastNamePaternal + ' ' + ISNULL(p.management_person_LastNameMaternal,'')),
                CASE
                    WHEN t.management_teacher_status = 1 THEN 'ALTA'
                    ELSE 'BAJA'
                END
            FROM dbo.management_teacher_table t
            INNER JOIN dbo.management_person_table p
                ON p.management_person_ID = t.management_teacher_PersonID
            LEFT JOIN dbo.management_user_table u
                ON u.management_user_PersonID = p.management_person_ID

            ORDER BY Fecha DESC;";

        private const string GetRolesQuery = @"
            SELECT management_role_ID, management_role_Name
            FROM dbo.management_role_table
            WHERE management_role_status = 1;";

        private readonly DbContext _context;

        public Management(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<T>> ExecuteStoredProcedureAsync<T>(
            string option,
            Dictionary<string, object>? parameters,
            Func<DbDataReader, T> mapFunction)
        {
            if (TryGetDashboardQuery(option, parameters, out var sql, out var directParameters))
            {
                return await ExecuteQueryAsync(sql, directParameters, mapFunction);
            }

            var results = new List<T>();
            var conn = _context.Database.GetDbConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "dbo.sp_management";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@Option", option));

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(new SqlParameter(param.Key, param.Value ?? DBNull.Value));
                }
            }

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            try
            {
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(mapFunction(reader));
                }
            }
            finally
            {
                await conn.CloseAsync();
            }

            return results;
        }

        public async Task<List<T>> ExecuteQueryAsync<T>(
            string sql,
            Dictionary<string, object>? parameters,
            Func<DbDataReader, T> mapFunction)
        {
            var results = new List<T>();
            var conn = _context.Database.GetDbConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(new SqlParameter(param.Key, param.Value ?? DBNull.Value));
                }
            }

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            try
            {
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(mapFunction(reader));
                }
            }
            finally
            {
                await conn.CloseAsync();
            }

            return results;
        }

        public async Task<int> ExecuteCommandAsync(string sql, Dictionary<string, object>? parameters)
        {
            var conn = _context.Database.GetDbConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(new SqlParameter(param.Key, param.Value ?? DBNull.Value));
                }
            }

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            try
            {
                return await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public async Task<int> CreatePersonAsync(Dictionary<string, object> parameters)
        {
            var result = await ExecuteStoredProcedureAsync(
                "management_person_insert",
                parameters,
                reader => GetValue<int>(reader, "management_person_ID")
            );

            return result.FirstOrDefault();
        }

        public async Task<int> CreateUserAsync(Dictionary<string, object> parameters)
        {
            var result = await ExecuteStoredProcedureAsync(
                "management_user_insert",
                parameters,
                reader => GetValue<int>(reader, "management_user_ID")
            );

            return result.FirstOrDefault();
        }

        public async Task<int> CreateStudentAsync(Dictionary<string, object> parameters)
        {
            var result = await ExecuteStoredProcedureAsync(
                "management_student_insert",
                parameters,
                reader => GetValue<int>(reader, "management_student_ID")
            );

            return result.FirstOrDefault();
        }

        public async Task<int> CreateTeacherAsync(Dictionary<string, object> parameters)
        {
            var result = await ExecuteStoredProcedureAsync(
                "management_teacher_insert",
                parameters,
                reader => GetValue<int>(reader, "management_teacher_ID")
            );

            return result.FirstOrDefault();
        }

        public async Task CreateGroupAsync(Dictionary<string, object> parameters)
        {
            await ExecuteStoredProcedureAsync(
                "management_group_insert",
                parameters,
                reader => 0
            );
        }

        public async Task CreateCareerAsync(Dictionary<string, object> parameters)
        {
            await ExecuteStoredProcedureAsync(
                "management_career_insert",
                parameters,
                reader => 0
            );
        }

        public async Task CreateUserRoleAsync(Dictionary<string, object> parameters)
        {
            await ExecuteStoredProcedureAsync(
                "management_userrole_insert",
                parameters,
                reader => 0
            );
        }

        public async Task CreateCycleAsync(Dictionary<string, object> parameters)
        {
            await ExecuteStoredProcedureAsync(
                "management_cycle_insert",
                parameters,
                reader => 0
            );
        }

        public async Task<Dictionary<int, string>> GetRolesAsync()
        {
            var roles = await ExecuteQueryAsync(GetRolesQuery, null, reader => new
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });

            return roles.ToDictionary(r => r.Id, r => r.Name);
        }

        public async Task<Dictionary<int, string>> GetCareersAsync()
        {
            var careers = await ExecuteStoredProcedureAsync(
                "management_career_get",
                null,
                reader => new
                {
                    Id = GetValue<int>(reader, "management_career_ID"),
                    Name = GetValue<string>(reader, "management_career_Name")
                });

            return careers
                .Where(c => c.Id > 0 && !string.IsNullOrWhiteSpace(c.Name))
                .GroupBy(c => c.Id)
                .ToDictionary(g => g.Key, g => g.First().Name!);
        }

        public async Task<List<BitacoraViewModel>> GetBitacoraAsync()
        {
            return await ExecuteQueryAsync(BitacoraQuery, null, reader => new BitacoraViewModel
            {
                Fecha = GetValue<DateTime>(reader, "Fecha"),
                Usuario = GetValue<string>(reader, "Usuario") ?? string.Empty,
                NombreCompleto = GetValue<string>(reader, "NombreCompleto") ?? string.Empty,
                Accion = GetValue<string>(reader, "Accion") ?? string.Empty
            });
        }

        public async Task ExecuteNonQueryAsync(string option, Dictionary<string, object> parameters)
        {
            var conn = _context.Database.GetDbConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "dbo.sp_management";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@Option", option));

            foreach (var param in parameters)
            {
                cmd.Parameters.Add(new SqlParameter(param.Key, param.Value ?? DBNull.Value));
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

        public static T? GetValue<T>(DbDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return default;

                object rawValue = reader.GetValue(ordinal);
                Type targetType = typeof(T);
                Type? underlying = Nullable.GetUnderlyingType(targetType);
                Type convertType = underlying ?? targetType;

                if (convertType.IsInstanceOfType(rawValue))
                {
                    if (underlying != null)
                    {
                        var nullableType = typeof(Nullable<>).MakeGenericType(underlying);
                        var boxedNullable = Activator.CreateInstance(nullableType, rawValue);
                        return (T)boxedNullable!;
                    }

                    return (T)rawValue;
                }

                var converted = Convert.ChangeType(rawValue, convertType);

                if (underlying != null)
                {
                    var nullableType = typeof(Nullable<>).MakeGenericType(underlying);
                    var boxedNullable = Activator.CreateInstance(nullableType, converted);
                    return (T)boxedNullable!;
                }

                return (T)converted!;
            }
            catch (IndexOutOfRangeException)
            {
                return default;
            }
        }

        private static bool TryGetDashboardQuery(
            string option,
            Dictionary<string, object>? parameters,
            out string sql,
            out Dictionary<string, object> directParameters)
        {
            sql = string.Empty;
            directParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            switch (option)
            {
                case "getview_student_full":
                    sql = StudentDashboardQuery;
                    directParameters["@ID"] = GetParameterValue(parameters, "@ID");
                    directParameters["@Status"] = GetParameterValue(parameters, "@Status");
                    directParameters["@StudentCareerID"] = GetParameterValue(parameters, "@StudentCareerID");
                    directParameters["@StudentGroupID"] = GetParameterValue(parameters, "@StudentGroupID");
                    directParameters["@Student_IsFolio"] = GetParameterValue(parameters, "@Student_IsFolio");
                    return true;

                case "getview_user_full":
                    sql = UserDashboardQuery;
                    directParameters["@ID"] = GetParameterValue(parameters, "@ID");
                    directParameters["@Status"] = GetParameterValue(parameters, "@Status");
                    directParameters["@UserPersonID"] = GetParameterValue(parameters, "@UserPersonID");
                    directParameters["@Username"] = GetParameterValue(parameters, "@Username");
                    directParameters["@UserEmail"] = GetParameterValue(parameters, "@UserEmail");
                    return true;

                case "management_group_get":
                    sql = GroupDashboardQuery;
                    directParameters["@ID"] = GetParameterValue(parameters, "@ID");
                    directParameters["@Status"] = GetParameterValue(parameters, "@Status");
                    directParameters["@GroupCareerID"] = GetParameterValue(parameters, "@GroupCareerID");
                    directParameters["@GroupCode"] = GetParameterValue(parameters, "@GroupCode");
                    return true;

                default:
                    return false;
            }
        }

        private static object GetParameterValue(Dictionary<string, object>? parameters, string key)
        {
            if (parameters != null && parameters.TryGetValue(key, out var value) && value != null)
            {
                return value;
            }

            return DBNull.Value;
        }
    }
}
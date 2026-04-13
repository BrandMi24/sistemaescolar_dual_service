using ControlEscolar.Data;
using ControlEscolar.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ControlEscolar.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Buscar usuario por email
            var user = _context.ManagementUsers.FirstOrDefault(u =>
                u.management_user_Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Usuario no encontrado");
                return View(model);
            }

            if (user.management_user_IsLocked)
            {
                ModelState.AddModelError("", "Usuario bloqueado");
                return View(model);
            }

            if (!user.management_user_status)
            {
                ModelState.AddModelError("", "Usuario inactivo");
                return View(model);
            }

            // Validar contraseña
            if (string.IsNullOrWhiteSpace(user.management_user_PasswordHash) || !VerifyPassword(model.Password, user.management_user_PasswordHash))
            {
                ModelState.AddModelError("", "Contraseña incorrecta");
                return View(model);
            }

            // Actualizar último login
            user.management_user_LastLoginDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.management_user_ID.ToString()),
                new Claim(ClaimTypes.Name, user.management_user_Email ?? "")
            };

            // Roles
            var rawRoles = await GetUserRoles(user.management_user_ID);
            if (rawRoles.Count == 0)
            {
                ModelState.AddModelError("", "El usuario no tiene roles activos.");
                return View(model);
            }

            var effectiveRoles = ExpandRoles(rawRoles);
            foreach (var role in effectiveRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            var (action, controller) = ResolveDefaultRoute(effectiveRoles);
            return RedirectToAction(action, controller);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // MÉTODOS AUXILIARES
        // =========================

        private bool VerifyPassword(string password, string hash)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            var inputHash = Convert.ToBase64String(bytes);
            return inputHash == hash;
        }

        private async Task<List<string>> GetUserRoles(int userId)
        {
                        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var conn = _context.Database.GetDbConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                                SELECT DISTINCT RoleName
                                FROM
                                (
                                        SELECT r.management_role_Name AS RoleName
                                        FROM dbo.management_userrole_table ur
                                        INNER JOIN dbo.management_role_table r
                                                ON r.management_role_ID = ur.management_userrole_RoleID
                                        WHERE ur.management_userrole_UserID = @UserID
                                            AND ur.management_userrole_status = 1
                                            AND r.management_role_status = 1

                                        UNION

                                        SELECT r.management_role_Name AS RoleName
                                        FROM dbo.management_user_table u
                                        INNER JOIN dbo.management_role_table r
                                                ON r.management_role_ID = u.management_user_RoleID
                                        WHERE u.management_user_ID = @UserID
                                            AND u.management_user_status = 1
                                            AND r.management_role_status = 1
                                ) x
                                WHERE x.RoleName IS NOT NULL
                                    AND LTRIM(RTRIM(x.RoleName)) <> ''";

            cmd.Parameters.Add(new SqlParameter("@UserID", userId));

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                    roles.Add(reader.GetString(0).Trim());
            }

            await conn.CloseAsync();

            return roles.ToList();
        }

        private static IReadOnlyCollection<string> ExpandRoles(IEnumerable<string> roles)
        {
            var expandedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var role in roles)
            {
                foreach (var alias in GetRoleAliases(role))
                {
                    expandedRoles.Add(alias);
                }
            }

            return expandedRoles.ToList();
        }

        private static IEnumerable<string> GetRoleAliases(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                yield break;

            var normalized = role.Trim();
            yield return normalized;

            switch (normalized.ToUpperInvariant())
            {
                case "ADMIN":
                case "ADMINISTRATOR":
                case "MASTER":
                    yield return "Admin";
                    yield return "Administrator";
                    yield return "Master";
                    break;

                case "COORDINADOR":
                case "SERVICELEARNINGCOORDINATOR":
                    yield return "Coordinador";
                    yield return "ServiceLearningCoordinator";
                    break;

                case "MAESTRO":
                case "TEACHER":
                case "DOCENTE":
                    yield return "Maestro";
                    yield return "Teacher";
                    break;

                case "TUTOR":
                    yield return "Tutor";
                    yield return "Teacher";
                    break;

                case "ASESOR":
                case "ASESORACADEMICO":
                case "ACADEMICSUPERVISOR":
                    yield return "Asesor";
                    yield return "AsesorAcademico";
                    yield return "AcademicSupervisor";
                    break;

                case "ALUMNO":
                case "STUDENT":
                    yield return "Alumno";
                    yield return "Student";
                    break;

                case "ADMISIONES":
                case "PREINSCRIPCIONES":
                    yield return "Admisiones";
                    yield return "Preinscripciones";
                    break;

                case "ENFERMERIA":
                case "ENFERMERÍA":
                    yield return "Enfermeria";
                    break;

                case "DIRECTOR":
                    yield return "Director";
                    break;

                case "ADMINISTRATIVO":
                    yield return "Administrativo";
                    break;
            }
        }

        private static (string Action, string Controller) ResolveDefaultRoute(IReadOnlyCollection<string> roles)
        {
            bool HasAny(params string[] roleNames)
                => roleNames.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase));

            if (HasAny("Admin", "Administrator", "Master"))
                return ("Index", "Admin");

            if (HasAny("Director"))
                return ("Index", "Dashboard");

            if (HasAny("Coordinador", "AcademicSupervisor"))
                return ("Index", "Coordinador");

            if (HasAny("AsesorAcademico", "Asesor", "Tutor", "Teacher", "Maestro"))
                return ("AlumnosAsignados", "AsesorAcademico");

            if (HasAny("Admisiones"))
                return ("Index", "Admisiones");

            if (HasAny("Preinscripciones"))
                return ("Index", "Preinscripciones");

            if (HasAny("Alumno", "Student"))
                return ("Index", "Alumno");

            return ("Index", "Home");
        }
    }
}
using ControlEscolar.Data;
using ControlEscolar.Helpers;
using ControlEscolar.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ControlEscolar.Controllers
{
    public class AccountController : Controller
    {
        private static readonly HashSet<string> AdvisorAreaRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "ASESORACADEMICO",
            "COORDINADOR",
            "COORDINADORDUAL",
            "COORDINADORSERVICIOSOCIAL",
            "ADMIN"
        };

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
            if (string.IsNullOrWhiteSpace(user.management_user_PasswordHash) ||
                !VerifyPassword(model.Password, user.management_user_PasswordHash))
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
                new Claim("UserId", user.management_user_ID.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.management_user_ID.ToString()),
                new Claim(ClaimTypes.Name, user.management_user_Email ?? "")
            };

            // Roles
            var roles = await GetUserRoles(user.management_user_ID);
            var expandedRoles = ExpandRoles(roles);
            foreach (var role in expandedRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // PRIORIDAD: returnUrl solo si es compatible con el rol autenticado.
            if (!string.IsNullOrEmpty(returnUrl) && IsReturnUrlAllowedForRoles(returnUrl, expandedRoles))
            {
                return LocalRedirect(returnUrl);
            }

            // REDIRECCIÓN DINÁMICA
            var (controller, action) = RoleRedirectHelper.GetRedirect(expandedRoles);

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
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        private bool VerifyPassword(string password, string hash)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            var inputHash = Convert.ToBase64String(bytes);
            return inputHash == hash;
        }

        private async Task<List<string>> GetUserRoles(int userId)
        {
            var roles = new List<string>();
            var conn = _context.Database.GetDbConnection();

            try
            {
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                // 1. Roles desde management_userrole_table (tabla relacional)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                SELECT r.management_role_Name
                FROM dbo.management_userrole_table ur
                INNER JOIN dbo.management_role_table r
                    ON r.management_role_ID = ur.management_userrole_RoleID
                WHERE ur.management_userrole_UserID = @UserID
                  AND ur.management_userrole_status = 1
                  AND r.management_role_status = 1";

                    cmd.Parameters.Add(new SqlParameter("@UserID", userId));

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                            roles.Add(reader.GetString(0));
                    }
                }

                // 2. Fallback: si no tiene roles en la tabla relacional,
                //    leer el RoleID directo de management_user_table
                if (roles.Count == 0)
                {
                    using var cmd2 = conn.CreateCommand();
                    cmd2.CommandText = @"
                SELECT r.management_role_Name
                FROM dbo.management_user_table u
                INNER JOIN dbo.management_role_table r
                    ON r.management_role_ID = u.management_user_RoleID
                WHERE u.management_user_ID = @UserID
                  AND u.management_user_RoleID IS NOT NULL
                  AND r.management_role_status = 1";

                    cmd2.Parameters.Add(new SqlParameter("@UserID", userId));

                    using var reader2 = await cmd2.ExecuteReaderAsync();
                    while (await reader2.ReadAsync())
                    {
                        if (!reader2.IsDBNull(0))
                            roles.Add(reader2.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo roles para usuario {userId}: {ex.Message}");
                return new List<string>();
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }
            return roles;
        }

        private static List<string> ExpandRoles(IEnumerable<string> roles)
        {
            var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                var trimmed = role.Trim();
                expanded.Add(trimmed);

                var compact = RemoveRoleSeparators(RemoveDiacritics(trimmed));
                if (!string.IsNullOrWhiteSpace(compact))
                    expanded.Add(compact);

                var normalized = RemoveRoleSeparators(RemoveDiacritics(trimmed)).ToUpperInvariant();
                if (normalized == "ACADEMICSUPERVISOR")
                {
                    expanded.Add("AsesorAcademico");
                }

                if (normalized == "ASESORACADEMICO")
                {
                    // Backward-compatible alias used by existing views/layout checks.
                    expanded.Add("AsesorAcademico");
                }

                if (normalized == "COORDINADORMODULODUAL" || normalized == "COORDINADORDUALMODULE")
                {
                    expanded.Add("COORDINADORDUAL");
                    expanded.Add("CoordinadorDual");
                }

                if (normalized == "COORDINADORDESERVICIOSOCIAL")
                {
                    expanded.Add("COORDINADORSERVICIOSOCIAL");
                    expanded.Add("COORDINADOR SERVICIO SOCIAL");
                    expanded.Add("CoordinadorServicioSocial");
                }

                if (normalized == "COORDINADORSERVICIOSOCIAL")
                {
                    expanded.Add("COORDINADOR SERVICIO SOCIAL");
                    expanded.Add("CoordinadorServicioSocial");
                }

                if (AdvisorAreaRoles.Contains(normalized))
                {
                    expanded.Add(normalized);
                }
            }

            return expanded.ToList();
        }

        private static bool IsReturnUrlAllowedForRoles(string returnUrl, List<string> roles)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return false;
            }

            var normalizedRoles = new HashSet<string>(roles.Select(r => RemoveRoleSeparators(RemoveDiacritics(r)).ToUpperInvariant()));
            if (normalizedRoles.Contains("ADMIN") || normalizedRoles.Contains("MASTER") || normalizedRoles.Contains("ADMINISTRATOR"))
            {
                return true;
            }

            var url = returnUrl.Trim();
            if (url.StartsWith("/Coordinador", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedRoles.Contains("COORDINADOR")
                    || normalizedRoles.Contains("COORDINADORDUAL")
                    || normalizedRoles.Contains("COORDINADORSERVICIOSOCIAL");
            }

            if (url.StartsWith("/AsesorAcademico", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedRoles.Contains("ASESORACADEMICO")
                    || normalizedRoles.Contains("COORDINADOR")
                    || normalizedRoles.Contains("COORDINADORDUAL")
                    || normalizedRoles.Contains("COORDINADORSERVICIOSOCIAL");
            }

            if (url.StartsWith("/Docente", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedRoles.Contains("DOCENTE") || normalizedRoles.Contains("TEACHER");
            }

            if (url.StartsWith("/Alumno", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedRoles.Contains("ALUMNO")
                    || normalizedRoles.Contains("STUDENT");
            }

            // Home/u otras rutas públicas
            return true;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string RemoveRoleSeparators(string text)
        {
            return text.Replace(" ", string.Empty)
                       .Replace("_", string.Empty)
                       .Replace("-", string.Empty);
        }
    }
}

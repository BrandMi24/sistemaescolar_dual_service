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
            if (!VerifyPassword(model.Password, user.management_user_PasswordHash))
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
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return LocalRedirect(returnUrl ?? Url.Action("Index", "Admin")!);
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

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT r.management_role_Name
                FROM dbo.management_userrole_table ur
                INNER JOIN dbo.management_role_table r
                    ON r.management_role_ID = ur.management_userrole_RoleID
                WHERE ur.management_userrole_UserID = @UserID
                  AND ur.management_userrole_status = 1
                  AND r.management_role_status = 1";

            cmd.Parameters.Add(new SqlParameter("@UserID", userId));

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                    roles.Add(reader.GetString(0));
            }

            await conn.CloseAsync();

            return roles;
        }
    }
}
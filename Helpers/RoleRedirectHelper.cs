using System.Globalization;
using System.Text;

namespace ControlEscolar.Helpers
{
    public static class RoleRedirectHelper
    {
        public static (string controller, string action) GetRedirect(List<string> roles)
        {
            // MAPA CENTRALIZADO
            var roleRoutes = new Dictionary<string, (string controller, string action)>
        {
            { "COORDINADORSERVICIOSOCIAL", ("Coordinador", "Index") },
            { "COORDINADORDUAL", ("Coordinador", "Index") },
            { "ADMIN", ("Coordinador", "Catalogos") },
            { "COORDINADOR", ("Coordinador", "Catalogos") },
            { "DOCENTE", ("Docente", "Index") },
            { "TEACHER", ("Docente", "Index") },
            { "ALUMNO", ("Alumno", "Index") },
            { "STUDENT", ("Alumno", "Index") },
            { "ASESORACADEMICO", ("AsesorAcademico", "Index") }
        };

            // Buscar primer rol válido
            foreach (var role in roles)
            {
                var key = NormalizeRoleKey(role);
                if (roleRoutes.ContainsKey(key))
                    return roleRoutes[key];
            }

            // DEFAULT (fallback)
            return ("Home", "Index");
        }

        private static string NormalizeRoleKey(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return string.Empty;

            var normalized = role.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    if (ch != ' ' && ch != '_' && ch != '-')
                    {
                        builder.Append(ch);
                    }
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }
    }
}

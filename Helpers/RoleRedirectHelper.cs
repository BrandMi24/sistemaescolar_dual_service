using System.Globalization;
using System.Text;

namespace ControlEscolar.Helpers
{
    public static class RoleRedirectHelper
    {
        private static readonly Dictionary<string, (string controller, string action)> RoleRoutes = new()
        {
            { "COORDINADORSERVICIOSOCIAL", ("AsesorAcademico", "Index") },
            { "COORDINADORDESERVICIOSOCIAL", ("AsesorAcademico", "Index") },
            { "COORDINADORDUAL", ("AsesorAcademico", "Index") },
            { "COORDINADORMODULODUAL", ("AsesorAcademico", "Index") },
            { "ADMIN", ("Coordinador", "Catalogos") },
            { "COORDINADOR", ("Coordinador", "Catalogos") },
            { "DOCENTE", ("Docente", "Index") },
            { "TEACHER", ("Docente", "Index") },
            { "ALUMNO", ("Alumno", "Index") },
            { "STUDENT", ("Alumno", "Index") },
            { "ASESORACADEMICO", ("AsesorAcademico", "Index") }
        };

        public static (string controller, string action) GetRedirect(List<string> roles)
        {
            foreach (var role in roles)
            {
                var key = NormalizeRoleKey(role);
                if (RoleRoutes.TryGetValue(key, out var route))
                    return route;
            }

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

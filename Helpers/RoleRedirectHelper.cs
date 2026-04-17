using Microsoft.AspNetCore.Mvc;

namespace ControlEscolar.Helpers
{
    public static class RoleRedirectHelper
    {
        public static (string controller, string action) GetRedirect(List<string> roles)
        {
            // MAPA CENTRALIZADO
            var roleRoutes = new Dictionary<string, (string controller, string action)>
        {
            { "ADMIN", ("Coordinador", "Index") },
            { "COORDINADOR", ("Coordinador", "Index") },
            { "COORDINADORSERVICIOSOCIAL", ("Coordinador", "Index") },
            { "COORDINADORDUAL", ("Coordinador", "Index") },
            { "DOCENTE", ("Docente", "Index") },
            { "TEACHER", ("Docente", "Index") },
            { "ALUMNO", ("Alumno", "Index") },
            { "STUDENT", ("Alumno", "Index") }
        };

            // Buscar primer rol válido
            foreach (var role in roles)
            {
                if (roleRoutes.ContainsKey(role.ToUpper()))
                    return roleRoutes[role.ToUpper()];
            }

            // DEFAULT (fallback)
            return ("Home", "Index");
        }
    }
}

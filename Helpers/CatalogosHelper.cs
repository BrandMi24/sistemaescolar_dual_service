using Microsoft.AspNetCore.Mvc.Rendering;

namespace ControlEscolar.Helpers
{
    /// <summary>
    /// Catálogos centralizados para los formularios.
    /// Fuente única de verdad para todas las listas de opciones (dropdowns).
    /// </summary>
    public static class CatalogosHelper
    {
        #region Carreras

        /// <summary>
        /// Lista normalizada de carreras disponibles (con acentos).
        /// Se acepta también la versión sin acentos para compatibilidad con datos existentes.
        /// </summary>
        public static readonly string[] Carreras =
        {
            "INGENIERÍA MECATRÓNICA",
            "INGENIERÍA EN DESARROLLO Y GESTIÓN DE SOFTWARE",
            "INGENIERÍA EN PROCESOS Y OPERACIONES INDUSTRIALES"
        };

        #endregion

        #region Datos Personales

        public static readonly string[] Sexos = { "MASCULINO", "FEMENINO" };

        public static readonly string[] EstadosCiviles =
        {
            "Soltero", "Casado", "Divorciado", "Unión Libre", "Viudo"
        };

        public static readonly string[] Nacionalidades =
        {
            "MEXICANA", "ESTADOUNIDENSE", "CUBANA", "FRANCESA",
            "GUATEMALTECA", "COSTARICENSE", "COLOMBIANA", "PANAMEÑA",
            "DOMINICANA", "VENEZOLANA", "HAITIANA", "HONDUREÑA",
            "PERUANA", "NIGERIANA", "SENEGALES"
        };

        #endregion

        #region Datos Escolares

        public static readonly string[] SistemasEstudio =
        {
            "Por Cooperación", "Privada", "Pública"
        };

        public static readonly string[] TiposPreparatoria =
        {
            "Bachillerato Abierto", "Bachillerato General",
            "Bachillerato Tecnológico", "C.H.H.",
            "Colegio de Bachilleres", "Otro",
            "Preparatoria Universitaria", "Profesional Técnico"
        };

        public static readonly string[] OpcionesEducativas = { "PRIMERA", "SEGUNDA" };

        #endregion

        #region Otros Datos

        public static readonly string[] TiposBeca =
        {
            "BENITO JUÁREZ", "BIENESTAR", "ESTATALES",
            "JOVENES CONSTRUYENDO EL FUTURO", "JOVENES ESCRIBIENDO EL FUTURO",
            "MUNICIPALES", "OTRO", "NO ABANDONO SEP"
        };

        public static readonly string[] MediosDifusion =
        {
            "Amigos", "Redes sociales", "Familiares", "Internet",
            "Visitaron tu Escuela", "Radio", "Radio / TV", "Televisión",
            "CORREO ELECTRÓNICO", "Correo electrónico",
            "OPEN HOUSE", "Publicidad Impresa", "Periodicos", "Otro"
        };

        public static readonly string[] Parentescos =
        {
            "Padre", "Madre", "Hermano", "Hermana",
            "Abuelo", "Abuela", "Tío", "Tía",
            "Tutor Académico", "Datos Emergencia"
        };

        #endregion

        #region Salud

        public static readonly string[] ServiciosMedicos =
        {
            "IMSS", "ISSSTE", "Centro de Salud", "Hospital General",
            "Cruz Roja", "Farmacia", "Particular", "Otro"
        };

        public static readonly string[] Discapacidades =
        {
            "Ninguna",
            "Discapacidad física / Motriz",
            "Discapacidad intelectual",
            "Discapacidad múltiple",
            "Discapacidad auditiva / Hipoacusia",
            "Discapacidad auditiva / Sordera",
            "Discapacidad visual / Baja visión",
            "Discapacidad visual / Ceguera",
            "Discapacidad psicosocial",
            "Depresión"
        };

        #endregion

        #region Helpers

        /// <summary>
        /// Genera una lista de SelectListItem a partir de un array de strings.
        /// </summary>
        public static List<SelectListItem> ToSelectList(string[] items, string? selectedValue = null, string placeholderText = "SIN SELECCION")
        {
            var list = new List<SelectListItem>
            {
                new SelectListItem(placeholderText, "")
            };

            foreach (var item in items)
            {
                list.Add(new SelectListItem(item, item, item == selectedValue));
            }

            return list;
        }

        /// <summary>
        /// Genera una lista de SelectListItem sin placeholder.
        /// </summary>
        public static List<SelectListItem> ToSelectListNoPlaceholder(string[] items, string? selectedValue = null)
        {
            var list = new List<SelectListItem>();
            foreach (var item in items)
            {
                list.Add(new SelectListItem(item, item, item == selectedValue));
            }
            return list;
        }

        #endregion
    }
}

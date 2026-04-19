using System.Linq;
using System.Data.Common;
using System.Text;
using ControlEscolar.Data;

namespace ControlEscolar.Models
{
    public static class ModelMappers
    {
        public static StudentViewModel MapToStudent(DbDataReader reader)
        {
            var groupCode = Management.GetValue<string>(reader, "management_group_Code")
                ?? Management.GetValue<string>(reader, "student_group");

            var groupName = Management.GetValue<string>(reader, "management_group_Name");

            var groupDisplay = !string.IsNullOrWhiteSpace(groupCode) && !string.IsNullOrWhiteSpace(groupName)
                ? $"{groupCode} - {groupName}"
                : groupCode ?? groupName ?? string.Empty;

            int? semestre = null;

            if (!string.IsNullOrEmpty(groupCode))
            {
                var numero = ExtractFirstNumberToken(groupCode);
                if (int.TryParse(numero, out int sem))
                    semestre = sem;
            }

            return new StudentViewModel
            {
                Id = Management.GetValue<int>(reader, "management_student_ID"),
                PersonId = Management.GetValue<int>(reader, "management_person_ID"),
                CareerId = Management.GetValue<int>(reader, "management_career_ID"),
                GroupId = Management.GetValue<int?>(reader, "management_group_ID"),
                Matricula = Management.GetValue<string>(reader, "management_student_Matricula")
                    ?? Management.GetValue<string>(reader, "student_Matricula"),
                Folio = Management.GetValue<string>(reader, "management_student_EnrollmentFolio"),
                IsFolio = Management.GetValue<bool?>(reader, "management_student_IsFolio"),
                Nombres = Management.GetValue<string>(reader, "management_person_FirstName") ?? "",
                ApellidoPaterno = Management.GetValue<string>(reader, "management_person_LastNamePaternal") ?? "",
                ApellidoMaterno = Management.GetValue<string>(reader, "management_person_LastNameMaternal"),
                Carrera = Management.GetValue<string>(reader, "management_career_Name") ?? "Sin Asignar",
                Semestre = semestre,
                EstadoCodigo = Management.GetValue<string>(reader, "management_student_StatusCode") ?? "",
                EsActivo = Management.GetValue<bool>(reader, "management_student_status"),
                CURP = Management.GetValue<string>(reader, "management_person_CURP")
                    ?? Management.GetValue<string>(reader, "person_CURP")
                    ?? Management.GetValue<string>(reader, "CURP"),
                Email = Management.GetValue<string>(reader, "management_person_Email")
                    ?? Management.GetValue<string>(reader, "person_email")
                    ?? Management.GetValue<string>(reader, "management_user_Email")
                    ?? Management.GetValue<string>(reader, "user_Email")
                    ?? Management.GetValue<string>(reader, "Email"),
                Phone = Management.GetValue<string>(reader, "management_person_Phone"),
                Grupo = groupDisplay
            };
        }

        public static GroupViewModel MapToGroup(DbDataReader reader)
        {
            var groupCode = Management.GetValue<string>(reader, "management_group_Code") ?? "";
            var digits = ExtractFirstNumberToken(groupCode);

            return new GroupViewModel
            {
                Id = Management.GetValue<int>(reader, "management_group_ID"),
                CareerId = Management.GetValue<int?>(reader, "management_group_CareerID"),
                Cuatrimestre = int.TryParse(digits, out var cuatrimestre) ? cuatrimestre : null,
                Carrera = Management.GetValue<string>(reader, "management_career_Name") ?? "Sin carrera",
                Codigo = groupCode,
                Nombre = Management.GetValue<string>(reader, "management_group_Name") ?? "",
                Turno = Management.GetValue<string>(reader, "management_group_Shift") ?? "",
                EsActivo = Management.GetValue<bool>(reader, "management_group_status")
            };
        }

        private static string ExtractFirstNumberToken(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var token = new StringBuilder();
            var found = false;

            foreach (var ch in input)
            {
                if (char.IsDigit(ch))
                {
                    token.Append(ch);
                    found = true;
                    continue;
                }

                if (found)
                {
                    break;
                }
            }

            return token.ToString();
        }

        public static HistoricoViewModel MapToHistorico(DbDataReader reader)
        {
            var nombreCompleto = (
                (Management.GetValue<string>(reader, "management_person_FirstName") ?? "") + " " +
                (Management.GetValue<string>(reader, "management_person_LastNamePaternal") ?? "") + " " +
                (Management.GetValue<string>(reader, "management_person_LastNameMaternal") ?? "")
            ).Trim();

            return new HistoricoViewModel
            {
                Id = Management.GetValue<int>(reader, "management_user_ID"),
                Usuario = Management.GetValue<string>(reader, "management_user_Username") ?? "",
                Email = Management.GetValue<string>(reader, "management_user_Email")
                    ?? Management.GetValue<string>(reader, "person_email")
                    ?? "",
                NombreCompleto = nombreCompleto,
                EsActivo = Management.GetValue<bool>(reader, "management_user_status"),
                Fecha = Management.GetValue<DateTime>(reader, "management_user_createdDate")
            };
        }

        public static DocenteViewModel MapToDocente(DbDataReader reader)
        {
            var estado = Management.GetValue<string>(reader, "teacher_statuscode")
                ?? (Management.GetValue<bool?>(reader, "teacher_status") == true ? "ACTIVO" : "INACTIVO");

            return new DocenteViewModel
            {
                UserId = Management.GetValue<int>(reader, "management_user_ID"),
                PersonId = Management.GetValue<int>(reader, "management_person_ID"),
                Email = Management.GetValue<string>(reader, "management_user_Email")
                    ?? Management.GetValue<string>(reader, "person_email")
                    ?? "",
                Nombre = Management.GetValue<string>(reader, "management_person_FirstName") ?? "",
                ApellidoPaterno = Management.GetValue<string>(reader, "management_person_LastNamePaternal") ?? "",
                ApellidoMaterno = Management.GetValue<string>(reader, "management_person_LastNameMaternal") ?? "",
                Telefono = Management.GetValue<string>(reader, "management_person_Phone") ?? "",
                TeacherId = Management.GetValue<int?>(reader, "teacher_ID"),
                NumeroEmpleado = Management.GetValue<string>(reader, "management_teacher_EmployeeNumber") ?? "",
                Estado = (estado ?? "ACTIVO").ToUpperInvariant()
            };
        }

        public static UsuarioViewModel MapToUsuario(DbDataReader reader)
        {
            var user = new UsuarioViewModel
            {
                Id = Management.GetValue<int?>(reader, "management_user_ID") ?? 0,
                PersonId = Management.GetValue<int?>(reader, "management_person_ID") ?? 0,
                Username = Management.GetValue<string>(reader, "management_user_Username"),
                Correo = Management.GetValue<string>(reader, "management_user_Email")
                    ?? Management.GetValue<string>(reader, "person_email")
                    ?? "",
                Roles = Management.GetValue<string>(reader, "Roles") ?? "Sin Rol",
                Carrera = Management.GetValue<string>(reader, "student_career") ?? "-",
                FirstName = Management.GetValue<string>(reader, "management_person_FirstName"),
                LastNamePaternal = Management.GetValue<string>(reader, "management_person_LastNamePaternal"),
                LastNameMaternal = Management.GetValue<string>(reader, "management_person_LastNameMaternal"),
                Grupo = Management.GetValue<string>(reader, "student_group")
                    ?? Management.GetValue<string>(reader, "management_group_Name")
                    ?? Management.GetValue<string>(reader, "management_group_Code")
                    ?? "-"
            };

            var matricula = Management.GetValue<string>(reader, "management_student_Matricula") ?? "";
            var folio = Management.GetValue<string>(reader, "management_student_EnrollmentFolio") ?? "";
            var empleado = Management.GetValue<string>(reader, "management_teacher_EmployeeNumber") ?? "";

            var userActivo = Management.GetValue<bool?>(reader, "management_user_status") ?? false;
            var studentStatus = Management.GetValue<int?>(reader, "student_status");
            var studentStatusCode = Management.GetValue<string>(reader, "student_statuscode")
                ?? Management.GetValue<string>(reader, "management_student_StatusCode");
            var teacherStatusCode = Management.GetValue<string>(reader, "teacher_statuscode");
            var teacherId = Management.GetValue<int?>(reader, "teacher_ID");
            var studentId = Management.GetValue<int?>(reader, "student_ID")
                ?? Management.GetValue<int?>(reader, "management_student_ID");

            if (!string.IsNullOrWhiteSpace(matricula) || !string.IsNullOrWhiteSpace(folio) || studentId.HasValue)
            {
                user.TipoUsuario = "ALUMNO";
                user.IdentificadorTipo = "ALUMNO";
                user.RelatedEntityId = studentId;
                user.Identificador = !string.IsNullOrWhiteSpace(matricula) ? matricula : (folio ?? "-");

                if (studentStatus == 0)
                    user.Estado = "BAJA";
                else
                    user.Estado = string.IsNullOrWhiteSpace(studentStatusCode) ? "INSCRITO" : studentStatusCode.ToUpperInvariant();
            }
            else if (!string.IsNullOrWhiteSpace(empleado) || teacherId.HasValue)
            {
                user.TipoUsuario = "DOCENTE";
                user.IdentificadorTipo = "DOCENTE";
                user.RelatedEntityId = teacherId;
                user.Identificador = !string.IsNullOrWhiteSpace(empleado) ? empleado : "-";
                user.Estado = string.IsNullOrWhiteSpace(teacherStatusCode)
                    ? (userActivo ? "ACTIVO" : "INACTIVO")
                    : teacherStatusCode.ToUpperInvariant();
            }
            else
            {
                user.TipoUsuario = "ADMIN";
                user.IdentificadorTipo = "ADMIN";
                user.RelatedEntityId = user.Id;
                user.Identificador = user.Username ?? user.Correo ?? "-";
                user.Estado = userActivo ? "ACTIVO" : "INACTIVO";
            }

            return user;
        }

        public static CycleViewModel MapToCycle(DbDataReader reader)
        {
            return new CycleViewModel
            {
                Id = Management.GetValue<int>(reader, "management_cycle_ID"),
                Name = Management.GetValue<string>(reader, "management_cycle_Name") ?? "",
                StartDate = Management.GetValue<DateTime>(reader, "management_cycle_StartDate"),
                EndDate = Management.GetValue<DateTime>(reader, "management_cycle_EndDate"),
                StatusCode = Management.GetValue<string>(reader, "management_cycle_StatusCode") ?? ""
            };
        }
    }
}
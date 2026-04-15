using System.Linq;
using System.Data.Common;
using ControlEscolar.Data;

namespace ControlEscolar.Models
{
	public static class ModelMappers
	{
		// 1. Mapeador para Alumnos, Bajas y Nuevo Ingreso
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
                var numero = new string(groupCode.TakeWhile(char.IsDigit).ToArray());

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
			return new GroupViewModel
			{
				Id = Management.GetValue<int>(reader, "management_group_ID"),
				CareerId = Management.GetValue<int>(reader, "management_group_CareerID"),
				Carrera = Management.GetValue<string>(reader, "management_career_Name") ?? "Sin carrera",
				Codigo = Management.GetValue<string>(reader, "management_group_Code") ?? "",
				Nombre = Management.GetValue<string>(reader, "management_group_Name") ?? "",
				Turno = Management.GetValue<string>(reader, "management_group_Shift") ?? "",
				EsActivo = Management.GetValue<bool>(reader, "management_group_status")
			};
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

		// 2. Mapeador para Docentes
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
                Estado = (estado ?? "ACTIVO").ToUpper()
            };
		}

		// 3. Mapeador para la vista general de Usuarios (Administración)
		public static UsuarioViewModel MapToUsuario(DbDataReader reader)
		{
			var user = new UsuarioViewModel
			{
				Id = Management.GetValue<int>(reader, "management_user_ID"),
                Username = Management.GetValue<string>(reader, "management_user_Username"),
                Correo = Management.GetValue<string>(reader, "management_user_Email")
					?? Management.GetValue<string>(reader, "person_email")
					?? "",
				// Roles en la BD puede venir como "Roles"
				Roles = Management.GetValue<string>(reader, "Roles") ?? "Sin Rol",
				Carrera = Management.GetValue<string>(reader, "student_career") ?? "-",
			};

            // Nombre completo: 
            user.FirstName = Management.GetValue<string>(reader, "management_person_FirstName");
            user.LastNamePaternal = Management.GetValue<string>(reader, "management_person_LastNamePaternal");
            user.LastNameMaternal = Management.GetValue<string>(reader, "management_person_LastNameMaternal");

            // Grupo: intentamos varias columnas comunes
            user.Grupo = Management.GetValue<string>(reader, "student_group")
				?? Management.GetValue<string>(reader, "management_group_Name")
				?? Management.GetValue<string>(reader, "management_group_Code")
				?? (Management.GetValue<int?>(reader, "group_Grade")?.ToString())
				?? "-";

            // Lógica de Identificador (Matrícula > Folio > Empleado)
            string matricula = Management.GetValue<string>(reader, "management_student_Matricula") ?? "";
            string folio = Management.GetValue<string>(reader, "management_student_EnrollmentFolio") ?? "";
            string empleado = Management.GetValue<string>(reader, "management_teacher_EmployeeNumber") ?? "";

            bool userActivo = Management.GetValue<bool?>(reader, "management_user_status") ?? false;

            // ESTADOS REALES
            int? studentStatus = Management.GetValue<int?>(reader, "student_status");
            string studentStatusCode = Management.GetValue<string>(reader, "management_student_StatusCode");
            string teacherStatusCode = Management.GetValue<string>(reader, "teacher_statuscode");

            // ---------------------------
            // ESTADO FINAL
            // ---------------------------
            if (!string.IsNullOrEmpty(matricula) || !string.IsNullOrEmpty(folio))
            {
                if (studentStatus == 0)
                    user.Estado = "BAJA";
                else
                    user.Estado = studentStatusCode ?? "ACTIVO";
            }
            else if (!string.IsNullOrEmpty(empleado))
            {
                // Docente
                user.Estado = teacherStatusCode ?? "ACTIVO";
            }
            else
            {
                // Usuario admin
                user.Estado = userActivo ? "ACTIVO" : "INACTIVO";
            }

            return user;
		}

        public static CycleViewModel MapToCycle(DbDataReader reader)
        {
            return new CycleViewModel
            {
                Id = Management.GetValue<int>(reader, "management_cycle_ID"),
                Name = Management.GetValue<string>(reader, "management_cycle_Name"),
                StartDate = Management.GetValue<DateTime>(reader, "management_cycle_StartDate"),
                EndDate = Management.GetValue<DateTime>(reader, "management_cycle_EndDate"),
                StatusCode = Management.GetValue<string>(reader, "management_cycle_StatusCode")
            };
        }
    }
}
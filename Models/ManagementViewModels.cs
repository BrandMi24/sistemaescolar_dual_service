using System;
using System.ComponentModel.DataAnnotations;

namespace ControlEscolar.Models
{
    public class StudentViewModel
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int CareerId { get; set; }
        public int PersonId { get; set; }
        public int? GroupId { get; set; }
        public string? Matricula { get; set; }
        public string? Folio { get; set; }
        public bool? IsFolio { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string? ApellidoMaterno { get; set; }
        public string Carrera { get; set; } = string.Empty;
        public int? Semestre { get; set; }
        public string EstadoCodigo { get; set; } = string.Empty;
        public bool EsActivo { get; set; }
        public string? CURP { get; set; }
        public string? Email { get; set; }
        public string? Grupo { get; set; }
        public string? Phone { get; set; }
    }

    public class GroupViewModel
    {
        public int Id { get; set; }
        public int? CareerId { get; set; }
        public string Carrera { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;
        public bool EsActivo { get; set; }
    }

    public class HistoricoViewModel
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;
        public bool EsActivo { get; set; }
        public DateTime Fecha { get; set; }
        public string Accion { get; set; } = string.Empty;
    }

    public class DocenteViewModel
    {
        public int PersonId { get; set; }
        public int UserId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public int? TeacherId { get; set; }
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class UsuarioViewModel
    {
        public int PersonId { get; set; }
        public int Id { get; set; }
        public string? Username { get; set; }
        public string Correo { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastNamePaternal { get; set; }
        public string? LastNameMaternal { get; set; }
        public string Grupo { get; set; } = string.Empty;
        public string Identificador { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int? RelatedEntityId { get; set; }
        public string IdentificadorTipo { get; set; } = string.Empty;
    }

    public class BitacoraViewModel
    {
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
    }

    public class CreateCareerViewModel
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;
    }

    public class CreateUserViewModel
    {
        public int? UserId { get; set; }
        public int? PersonId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastNamePaternal { get; set; } = string.Empty;

        public string? LastNameMaternal { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Department { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        public string? Password { get; set; }

        // NUEVO: vincular a un alumno existente
        public int? ExistingStudentId { get; set; }
    }

    public class CreateStudentViewModel
    {
        public int? StudentId { get; set; }
        public int? PersonId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string LastNamePaternal { get; set; } = string.Empty;
        public string? LastNameMaternal { get; set; }
        public string? Phone { get; set; }

        public string? CURP { get; set; }
        public string? Matricula { get; set; }
        public string StatusCode { get; set; } = "INSCRITO";

        [Required]
        public int CareerId { get; set; }

        public int? GroupId { get; set; }

        public string? Username { get; set; }
        public string? Password { get; set; }

        // NUEVO: vincular cuenta existente
        public int? ExistingUserId { get; set; }
    }

    public class CreateTeacherViewModel
    {
        public int? TeacherId { get; set; }
        public int? PersonId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastNamePaternal { get; set; } = string.Empty;

        public string? LastNameMaternal { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? EmployeeNumber { get; set; }
        public string StatusCode { get; set; } = "ACTIVO";
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class CreateGroupViewModel
    {
        public int? Id { get; set; }

        [Required]
        public string GroupCode { get; set; } = string.Empty;

        [Required]
        public string GroupName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar una carrera")]
        public int? CareerId { get; set; }

        [Required]
        public string Shift { get; set; } = "MATUTINO";

        public bool IsActive { get; set; } = true;
    }

    public class CycleViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string StatusCode { get; set; } = "ACTIVO";
    }
}
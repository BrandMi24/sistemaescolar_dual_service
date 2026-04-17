using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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

        [EmailAddress]
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

        [EmailAddress]
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
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class CreateUserViewModel
    {
        public int? UserId { get; set; }
        public int? PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastNamePaternal { get; set; } = string.Empty;
        public string? LastNameMaternal { get; set; }

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Department { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public int? ExistingStudentId { get; set; }
    }

    public class CreateStudentViewModel
    {
        public int? StudentId { get; set; }
        public int? PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string LastNamePaternal { get; set; } = string.Empty;
        public string? LastNameMaternal { get; set; }
        public string? Phone { get; set; }
        public string? CURP { get; set; }
        public string? Matricula { get; set; }
        public string StatusCode { get; set; } = "INSCRITO";
        public int CareerId { get; set; }
        public int? GroupId { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int? ExistingUserId { get; set; }
    }

    public class ImportStudentsCsvViewModel
    {
        [Display(Name = "Archivo CSV")]
        public IFormFile? CsvFile { get; set; }

        public string ReturnTab { get; set; } = "tab-alumnos";

        public List<ImportStudentCsvResultViewModel> Results { get; set; } = new();

        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
    }

    public class ImportStudentCsvRowViewModel
    {
        public int RowNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastNamePaternal { get; set; } = string.Empty;
        public string? LastNameMaternal { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? CURP { get; set; }
        public int CareerId { get; set; }
        public int? GroupId { get; set; }
        public string StatusCode { get; set; } = "INSCRITO";
    }

    public class ImportStudentCsvResultViewModel
    {
        public int RowNumber { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? PersonId { get; set; }
        public int? StudentId { get; set; }
        public string? Matricula { get; set; }
        public string? Folio { get; set; }
    }

    public class CreateTeacherViewModel
    {
        public int? TeacherId { get; set; }
        public int? PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
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
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int? CareerId { get; set; }
        public string Shift { get; set; } = "MATUTINO";
        public bool IsActive { get; set; } = true;
    }

    public class CycleViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StatusCode { get; set; } = "ACTIVO";
    }

    public class ToggleCycleStatusRequest
    {
        public int Id { get; set; }
        public bool Activar { get; set; }
    }

    public class CreateRoleViewModel
    {
        public int? RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
    }

    public class StudentOption
    {
        public string Display { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastNamePaternal { get; set; } = string.Empty;
        public string? LastNameMaternal { get; set; }
        public string? Email { get; set; }
    }
}
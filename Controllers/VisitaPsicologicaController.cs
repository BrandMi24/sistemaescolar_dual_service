using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ControlEscolar.Data;
using ControlEscolar.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ControlEscolar.Controllers
{
    [Authorize(Roles = "Psychologist,Head Nurse")]
    public class VisitaPsicologicaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VisitaPsicologicaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(VisitaPsicologica model)
        {
            ModelState.Remove("NombreCompleto");
            ModelState.Remove("Carrera");

            if (!ModelState.IsValid) return View(model);

            model.FechaVisita = DateTime.Now;
            _context.VisitasPsicologicas.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDatosAlumno(string matricula)
        {
            if (string.IsNullOrEmpty(matricula)) return BadRequest("Matrícula vacía");

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                var query = @"
                SELECT 
                    (dp.academiccontrol_preinscription_personaldata_name + ' ' + dp.academiccontrol_preinscription_personaldata_paternalSurname + ' ' + dp.academiccontrol_preinscription_personaldata_maternalSurname) AS NombreCompleto,
                    p.academiccontrol_preinscription_careerRequested AS Carrera,
                    CONVERT(varchar, dp.academiccontrol_preinscription_personaldata_birthDate, 23) AS FechaNacimiento
                FROM academiccontrol_inscription_table i
                INNER JOIN academiccontrol_preinscription_table p ON i.academiccontrol_inscription_preinscriptionID = p.academiccontrol_preinscription_ID
                INNER JOIN academiccontrol_preinscription_personaldata_table dp ON dp.academiccontrol_preinscription_personaldata_preinscriptionID = p.academiccontrol_preinscription_ID
                WHERE i.academiccontrol_inscription_enrollment = @matricula";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    var param = command.CreateParameter();
                    param.ParameterName = "@matricula";
                    param.Value = matricula;
                    command.Parameters.Add(param);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return Json(new
                            {
                                nombreCompleto = reader["NombreCompleto"].ToString(),
                                carrera = reader["Carrera"].ToString(),
                                fechaNacimiento = reader["FechaNacimiento"].ToString()
                            });
                        }
                    }
                }
            }
            return NotFound("No se encontró el alumno");
        }

        public async Task<IActionResult> Index()
        {
            var lista = new List<VisitaPsicologica>();
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                var query = @"
                SELECT 
                    v.Matricula,
                    MAX(v.FechaVisita) AS FechaVisita,
                    ISNULL(NULLIF(LTRIM(RTRIM(dp.academiccontrol_preinscription_personaldata_name + ' ' + dp.academiccontrol_preinscription_personaldata_paternalSurname + ' ' + dp.academiccontrol_preinscription_personaldata_maternalSurname)), ''), 'FALTA NOMBRE EN BD') AS NombreCompleto,
                    ISNULL(NULLIF(LTRIM(RTRIM(i.academiccontrol_inscription_careerRequested)), ''), 'FALTA CARRERA EN BD') AS Carrera
                FROM VisitasPsicologicas v
                LEFT JOIN academiccontrol_inscription_table i ON v.Matricula = i.academiccontrol_inscription_enrollment
                LEFT JOIN academiccontrol_preinscription_personaldata_table dp ON i.academiccontrol_inscription_preinscriptionID = dp.academiccontrol_preinscription_personaldata_preinscriptionID
                GROUP BY v.Matricula, dp.academiccontrol_preinscription_personaldata_name, dp.academiccontrol_preinscription_personaldata_paternalSurname, dp.academiccontrol_preinscription_personaldata_maternalSurname, i.academiccontrol_inscription_careerRequested";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new VisitaPsicologica
                            {
                                Matricula = reader["Matricula"].ToString(),
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Carrera = reader["Carrera"].ToString(),
                                FechaVisita = (DateTime)reader["FechaVisita"]
                            });
                        }
                    }
                }
            }
            return View(lista);
        }

        public async Task<IActionResult> History(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var historial = await _context.VisitasPsicologicas.Where(v => v.Matricula == id).OrderByDescending(v => v.FechaVisita).ToListAsync();
            ViewData["Matricula"] = id;
            return View(historial);
        }
    }
}
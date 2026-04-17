using ControlEscolar.Data;
using ControlEscolar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ControlEscolar.Controllers
{
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class TramitesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TramitesController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ====================================================================
        // MÉTODO FALTANTE: Obtiene el ID del usuario para el Stored Procedure
        // ====================================================================
        private async Task<int> GetLegacyUserIdAsync()
        {
            // 1. Obtenemos el ID directamente de los Claims que guardó el AccountController
            var userIdClaim = User.FindFirst("UserId")?.Value
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return 0; // No hay nadie logueado o la sesión no tiene el ID
            }

            // 2. Convertimos el string a int (Ej. de "24" a 24)
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            return 0;
        }

        // ==========================================
        // VISTAS DEL ALUMNO
        // ==========================================
        public async Task<IActionResult> MisTramites()
        {
            // Esperamos el ID de forma asíncrona sin bloquear el servidor
            int userIdActual = await GetLegacyUserIdAsync();

            // Convertimos la consulta a una lista de forma asíncrona (.ToListAsync())
            var historial = await _context.Set<DetalleSolicitudViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_solicitud_getbyalumno', @ID={userIdActual}")
                .ToListAsync(); // <-- Cambiamos .ToList() por .ToListAsync()

            return View(historial);
        }

        [HttpGet]
        public async Task<IActionResult> ValidarMatricula()
        {
            // --- BLOQUE DE DEBUG (Mantenlo para que en el servidor puedan ver qué pasa) ---
            Console.WriteLine("Tramites/ValidarMatricula - IsAuthenticated: " + User.Identity.IsAuthenticated);
            var debugClaims = User.Claims.Select(c => $"{c.Type} = {c.Value}").ToList();
            foreach (var c in debugClaims)
            {
                Console.WriteLine("Claim encontrado: " + c);
            }

            try
            {
                // Usamos el método asíncrono que busca tanto "UserId" como "NameIdentifier"
                int userIdActual = await GetLegacyUserIdAsync();

                if (userIdActual == 0)
                {
                    return Json(new { success = false, message = "Sesión no válida o usuario no identificado." });
                }

                // Ejecutamos el SP usando el ID de la sesión
                var infoAlumno = _context.Set<InfoAlumnoViewModel>()
                    .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_alumno_get_info_escolar', @ID={userIdActual}")
                    .AsEnumerable()
                    .FirstOrDefault();

                if (infoAlumno != null)
                {
                    return Json(new
                    {
                        success = true,
                        nombre = infoAlumno.Nombre,
                        matricula = infoAlumno.Matricula,
                        grado = infoAlumno.Grado,
                        grupo = infoAlumno.Grupo
                    });
                }

                return Json(new { success = false, message = "No se encontraron datos escolares." });
            }
            catch (Exception ex)
            {
                // Esto ayudará a ver el error real en la consola del navegador si algo truena
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        [HttpPost]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> GuardarSolicitud(List<IFormFile> archivos)
        {
            int userIdActual = await GetLegacyUserIdAsync();
            if (!int.TryParse(Request.Form["id_tramite"], out int idTramiteLocal))
                return BadRequest("ID de trámite no válido.");

            try
            {
                var info = _context.Set<InfoAlumnoViewModel>()
                    .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_alumno_get_info_escolar', @ID={userIdActual}")
                    .AsEnumerable().FirstOrDefault();

                if (info == null) return BadRequest("No se encontraron datos escolares.");

                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string nombreCarpetaMaestra = $"{info.Matricula}_{info.Nombre.Replace(" ", "_")}_{timeStamp}";
                string pathMaestra = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tramites", nombreCarpetaMaestra);
                string pathZipsRaiz = Path.Combine(pathMaestra, "Carpetas_ZIP");

                if (!Directory.Exists(pathMaestra)) Directory.CreateDirectory(pathMaestra);
                if (!Directory.Exists(pathZipsRaiz)) Directory.CreateDirectory(pathZipsRaiz);

                string nombreTramite = idTramiteLocal switch
                {
                    1 => "Tramite_de_Titulo",
                    2 => "Certificacion_de_Estudios",
                    3 => "Cursos_de_Extension",
                    4 => "Cursos_Empresariales",
                    5 => "Cursos_Regulares",
                    6 => "Inscripcion_Bachillerato",
                    7 => "Reconocimiento_Competencia",
                    _ => "Tramite_General"
                };

                string nombreCarpetaTramite = $"{nombreTramite.Replace(" ", "_")}_{timeStamp}";
                string folderPathTramite = Path.Combine(pathZipsRaiz, nombreCarpetaTramite);
                if (!Directory.Exists(folderPathTramite)) Directory.CreateDirectory(folderPathTramite);

                string rutaRelativaBD = Path.Combine(nombreCarpetaMaestra, "Carpetas_ZIP", nombreCarpetaTramite);

                var resultadoCabecera = _context.Set<TramiteResult>()
                    .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_solicitud_insert', @ID={userIdActual}, @TramiteID={idTramiteLocal}, @ArchivoPath={rutaRelativaBD}")
                    .AsEnumerable().FirstOrDefault();

                if (resultadoCabecera == null || resultadoCabecera.Id <= 0)
                    return BadRequest("Error al registrar la solicitud.");

                int nuevaSolicitudId = resultadoCabecera.Id;

                // USAMOS EL DBSET REAL AQUÍ
                var requisitos = await _context.TramitesRequisitos
                    .Where(r => r.id_tramite == idTramiteLocal)
                    .OrderBy(r => r.id_requisito).ToListAsync();

                int indexArchivoActual = 0;
                foreach (var req in requisitos)
                {
                    string estatusDoc = "Pendiente";
                    string? fileNameFinal = null;
                    bool requiereArchivo = !req.nombre_documento.ToLower().Contains("foto") && !req.nombre_documento.ToLower().Contains("fotografía");

                    if (requiereArchivo && archivos != null && indexArchivoActual < archivos.Count)
                    {
                        var archivoActual = archivos[indexArchivoActual];
                        fileNameFinal = $"{req.nombre_documento.Replace(" ", "_").Replace("/", "_")}_{timeStamp}{Path.GetExtension(archivoActual.FileName).ToLower()}";
                        using (var stream = new FileStream(Path.Combine(folderPathTramite, fileNameFinal), FileMode.Create))
                        {
                            await archivoActual.CopyToAsync(stream);
                        }
                        indexArchivoActual++;
                    }
                    else if (!requiereArchivo) estatusDoc = "Entrega Física";

                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"EXEC sp_tramites @Option='tramites_detalle_documento_insert', @ID={nuevaSolicitudId}, @RequisitoID={req.id_requisito}, @Estatus={estatusDoc}, @NombreArchivo={fileNameFinal}"
                    );
                }
                return Json(new { success = true, folio = nuevaSolicitudId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerRequisitosJson(int id)
        {
            var requisitos = await _context.Set<RequisitoSolicitudViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_categoria_get_requisitos', @ID={id}")
                .ToListAsync();
            return Json(requisitos.Select(r => new { id_requisito = r.IdRequisito, nombre_documento = r.NombreRequisito }).ToList());
        }

        [Authorize]
        public async Task<IActionResult> Gestion(string estatus = "Todos")
        {
            var listado = _context.Set<DetalleSolicitudViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_admin_get_solicitudes'")
                .AsEnumerable().ToList();

            if (estatus != "Todos") listado = listado.Where(x => x.Estatus == estatus).ToList();
            ViewBag.EstatusActual = estatus;
            return View(listado);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarEstatus(int id, string estatus, string observaciones)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_tramites @Option='tramites_solicitud_update_estatus', @ID={id}, @Estatus={estatus}, @Observaciones={observaciones}"
            );
            return RedirectToAction("Tramites", "Tutor");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarCarpeta(int idSolicitud, string matricula, string tipo)
        {
            var archivos = _context.Set<ArchivoDescargaViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_get_archivo_info', @ID={idSolicitud}")
                .AsEnumerable().ToList();

            if (!archivos.Any()) return NotFound("No hay archivos para descargar.");

            string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tramites", archivos.First().ArchivoPath);
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var doc in archivos)
                    {
                        string filePath = Path.Combine(folderPath, doc.NombreArchivoFisico);
                        if (System.IO.File.Exists(filePath)) archive.CreateEntryFromFile(filePath, doc.NombreArchivoFisico);
                    }
                }
                return File(memoryStream.ToArray(), "application/zip", $"{tipo.Replace(" ", "_")}_{matricula}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDetalleRequisitos(int idSolicitud)
        {
            var listado = await _context.Set<RequisitoRevisionViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_admin_get_requisitos_solicitud', @ID={idSolicitud}")
                .ToListAsync();
            return Json(listado);
        }

        [HttpGet]
        public async Task<IActionResult> DescargarArchivoIndividual(int idSolicitud, int idRequisito, string matricula)
        {
            var doc = _context.Set<ArchivoDescargaViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_get_archivo_info', @ID={idSolicitud}, @RequisitoID={idRequisito}")
                .AsEnumerable().FirstOrDefault();

            if (doc == null) return Json(new { success = false, message = "No se encontró el registro." });

            string path = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tramites", doc.ArchivoPath?.Trim() ?? "", doc.NombreArchivoFisico?.Trim() ?? "");
            if (!System.IO.File.Exists(path)) return Json(new { success = false, message = "Archivo físico no encontrado", rutaBuscada = path });

            var fileBytes = await System.IO.File.ReadAllBytesAsync(path);
            string extension = Path.GetExtension(doc.NombreArchivoFisico ?? "");
            return File(fileBytes, "application/octet-stream", $"{doc.NombreRequisito.Replace(" ", "_")}_{matricula}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarEstatusDocumento([FromBody] System.Text.Json.JsonElement data)
        {
            int idSol = data.GetProperty("idSolicitud").GetInt32();
            int idReq = data.GetProperty("idRequisito").GetInt32();
            string estatus = data.GetProperty("estatus").GetString() ?? "Pendiente";

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_tramites @Option='tramites_detalle_documento_actualizar_estatus', @ID={idSol}, @RequisitoID={idReq}, @Estatus={estatus}"
            );
            return Ok(new { success = true });
        }

        [HttpPost]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> SubsanarDocumento(int idDetalle, int idSolicitud, IFormFile archivoNuevo)
        {
            // Validaciones iniciales
            if (archivoNuevo == null || archivoNuevo.Length == 0)
                return Json(new { success = false, message = "Archivo no seleccionado." });

            // MANTENEMOS la validación de PDF de la Versión Actual
            if (archivoNuevo.ContentType != "application/pdf")
                return Json(new { success = false, message = "Solo se permiten archivos PDF." });

            // 1. Buscamos los registros en la base de datos
            var detalle = await _context.TramitesDetalleDocumentos
                .FirstOrDefaultAsync(d => d.id_solicitud == idSolicitud && d.id_requisito == idDetalle);
            var solicitud = await _context.TramitesSolicitudes.FindAsync(idSolicitud);
            var requisito = await _context.TramitesRequisitos.FindAsync(idDetalle);

            if (detalle == null || solicitud == null || requisito == null)
                return Json(new { success = false, message = "Datos no encontrados." });

            // 2. Definimos la ruta de la carpeta del trámite
            string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tramites", solicitud.tramites_solicitud_archivo_path);

            // 3. Borramos el archivo viejo si existe para no llenar el disco de basura
            if (!string.IsNullOrEmpty(detalle.nombre_archivo_fisico))
            {
                string oldPath = Path.Combine(folderPath, detalle.nombre_archivo_fisico);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            // 4. Guardamos el nuevo archivo con sufijo "CORREGIDO"
            string nuevoNombre = $"{requisito.nombre_documento.Replace(" ", "_")}_CORREGIDO_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            string fullPath = Path.Combine(folderPath, nuevoNombre);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await archivoNuevo.CopyToAsync(stream);
            }

            // 5. Notificamos a la base de datos mediante el SP
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_tramites @Option='tramites_documento_subsanar', @ID={idSolicitud}, @RequisitoID={idDetalle}, @NombreArchivo={nuevoNombre}"
            );

            return Json(new { success = true });
        }
    }
}
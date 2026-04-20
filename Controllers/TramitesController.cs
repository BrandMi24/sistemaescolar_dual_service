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
using ClosedXML.Excel;

namespace ControlEscolar.Controllers
{
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class TramitesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TramitesController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
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
        [RequestSizeLimit(10485760)]
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

                // RUTA PLANA: Sin subcarpeta Carpetas_ZIP
                string pathMaestra = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tramites", nombreCarpetaMaestra);

                if (!Directory.Exists(pathMaestra)) Directory.CreateDirectory(pathMaestra);

                // La ruta que guardamos en BD es simplemente el nombre de la carpeta (estructura plana)
                string rutaRelativaBD = nombreCarpetaMaestra;

                var resultadoCabecera = _context.Set<TramiteResult>()
                    .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_solicitud_insert', @ID={userIdActual}, @TramiteID={idTramiteLocal}, @ArchivoPath={rutaRelativaBD}")
                    .AsEnumerable().FirstOrDefault();

                if (resultadoCabecera == null || resultadoCabecera.Id <= 0)
                    return BadRequest("Error al registrar la solicitud.");

                int nuevaSolicitudId = resultadoCabecera.Id;

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

                        // Guardamos directamente en la carpeta del alumno
                        using (var stream = new FileStream(Path.Combine(pathMaestra, fileNameFinal), FileMode.Create))
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

            // Buscamos directo en la ruta base
            string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tramites", archivos.First().ArchivoPath);

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var doc in archivos)
                    {
                        // Solo unimos folderPath y nombre de archivo, sin subcarpetas extra
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
            try
            {
                int idSol = data.GetProperty("idSolicitud").GetInt32();
                int idReq = data.GetProperty("idRequisito").GetInt32();
                string estatus = data.GetProperty("estatus").GetString();

                // Ejecutar y capturar resultado
                var result = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_tramites @Option='tramites_detalle_documento_actualizar_estatus', @ID={idSol}, @RequisitoID={idReq}, @Estatus={estatus}"
                );

                if (result == 0) // El SP devolvió 0 filas afectadas
                {
                    return BadRequest("El SP no encontró el registro para actualizar. Revisa los IDs.");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost]
        [RequestSizeLimit(10485760)]
        public async Task<IActionResult> SubsanarDocumento(int idDetalle, int idSolicitud, IFormFile archivoNuevo)
        {
            try
            {
                // 1. Obtenemos la solicitud (esta sí debe existir siempre)
                var solicitud = await _context.TramitesSolicitudes
                    .FirstOrDefaultAsync(s => s.tramites_solicitud_id == idSolicitud);

                if (solicitud == null)
                    return Json(new { success = false, message = "Solicitud no encontrada." });

                // 2. Buscamos el detalle del documento
                var detalle = await _context.TramitesDetalleDocumentos
                    .FirstOrDefaultAsync(d => d.id_solicitud == idSolicitud && d.id_requisito == idDetalle);

                // 3. RUTA
                string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tramites", solicitud.tramites_solicitud_archivo_path);

                // 4. SI EL DETALLE YA EXISTÍA, BORRAMOS EL VIEJO FÍSICAMENTE
                if (detalle != null && !string.IsNullOrEmpty(detalle.nombre_archivo_fisico))
                {
                    string oldPath = Path.Combine(folderPath, detalle.nombre_archivo_fisico);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                // 5. GUARDAMOS EL ARCHIVO NUEVO
                string nuevoNombre = $"Doc_{idDetalle}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string fullPath = Path.Combine(folderPath, nuevoNombre);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await archivoNuevo.CopyToAsync(stream);
                }

                // 6. SI EL DETALLE NO EXISTÍA (es un campo nuevo), LO CREAMOS CON EL SP
                // Si el detalle YA existía, el SP lo actualizará.
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_tramites @Option='tramites_documento_subsanar', @ID={idSolicitud}, @RequisitoID={idDetalle}, @NombreArchivo={nuevoNombre}"
                );

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ActualizarDocumentoAlumno(int idSolicitud, int idRequisito, IFormFile archivoNuevo)
        {
            var solicitud = await _context.TramitesSolicitudes.FindAsync(idSolicitud);
            var detalle = await _context.TramitesDetalleDocumentos
                .FirstOrDefaultAsync(d => d.id_solicitud == idSolicitud && d.id_requisito == idRequisito);

            if (solicitud == null || detalle == null)
                return Json(new { success = false, message = "Error al localizar el trámite." });

            string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tramites", solicitud.tramites_solicitud_archivo_path);

            // 1. GENERAR NOMBRE: Si ya tenía uno, lo usamos; si no, creamos uno nuevo.
            string nombreArchivo = detalle.nombre_archivo_fisico ?? $"Doc_{idRequisito}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            string fullPath = Path.Combine(folderPath, nombreArchivo);

            // 2. BORRADO CONDICIONAL: Solo borramos si el archivo físicamente existe
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            // 3. GUARDAR NUEVO
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await archivoNuevo.CopyToAsync(stream);
            }

            // 4. ACTUALIZAR BD: Incluimos el nombre del archivo por si antes era NULL
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE CE_TramitesDetalleDocumentos SET nombre_archivo_fisico={nombreArchivo}, estatus_documento='Pendiente', fecha_validacion=NULL WHERE id_solicitud={idSolicitud} AND id_requisito={idRequisito}"
            );

            return Json(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> RevertirEstatus([FromBody] int idSolicitud)
        {
            // Ejecutas un SP que cambie el estatus a 'Pendiente' y borre observaciones
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_tramites @Option='tramites_revertir_estatus', @ID={idSolicitud}"
            );
            return Json(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> GuardarNuevoTramite([FromBody] TramitesCRUDViewModel model)
        {
            try
            {
                // 1. Guardar la Categoría
                var nuevaCat = new Cat_Tramites { nombre_tramite = model.nombre };
                _context.CategoriasTramites.Add(nuevaCat);
                await _context.SaveChangesAsync();

                // 2. Guardar los Requisitos
                foreach (var req in model.listaRequisitos)
                {
                    var nuevoReq = new Requisito_Tramite
                    {
                        id_tramite = nuevaCat.id_tramite,
                        nombre_documento = req.nombre,
                        // Usamos la propiedad de tu DTO
                    };
                    _context.TramitesRequisitos.Add(nuevoReq);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ObtenerCategorias()
        {
            // Solo traemos los que están activos
            var categorias = await _context.CategoriasTramites
                .Where(c => c.activo == true) // <--- FILTRO CRUCIAL
                .Select(c => new { c.id_tramite, c.nombre_tramite })
                .ToListAsync();
            return Json(categorias);
        }
        [HttpPost]
        public async Task<IActionResult> EliminarTramite(int id)
        {
            try
            {
                var tramite = await _context.CategoriasTramites.FindAsync(id);
                if (tramite == null) return Json(new { success = false, message = "Trámite no encontrado." });

                // Simplemente cambiamos el estado
                tramite.activo = false;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Trámite desactivado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicion([FromBody] TramitesCRUDViewModel model)
        {
            try
            {
                // 1. Bloqueo solo si hay Pendientes (solicitudes vivas)
                bool tienePendientes = await _context.TramitesSolicitudes
                    .AnyAsync(s => s.id_tramite == model.id && s.tramites_solicitud_estatus == "Pendiente");
                if (tienePendientes)
                    return Json(new { success = false, message = "No puedes editar: hay solicitudes pendientes." });

                var tramite = await _context.CategoriasTramites.FindAsync(model.id);
                tramite.nombre_tramite = model.nombre;

                // 2. EDICIÓN SEGURA: En lugar de borrar todos y crear, actualizamos los que existen
                // y solo agregamos los nuevos.
                var existentes = await _context.TramitesRequisitos.Where(r => r.id_tramite == model.id).ToListAsync();

                // Marcamos para borrar solo los que ya no están en la lista nueva (si no tienen documentos asociados)
                // O mejor aún: solo permitimos renombrar.
                foreach (var req in model.listaRequisitos)
                {
                    var existente = existentes.FirstOrDefault(e => e.nombre_documento == req.nombre); // Lógica simplificada
                    if (existente == null)
                    {
                        _context.TramitesRequisitos.Add(new Requisito_Tramite { id_tramite = model.id, nombre_documento = req.nombre });
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(string estatus)
        {
            // 1. Obtenemos los datos filtrados (mismo proceso que tu tabla)
            var listado = _context.Set<DetalleSolicitudViewModel>()
                .FromSqlInterpolated($"EXEC sp_tramites @Option='tramites_admin_get_solicitudes'")
                .AsEnumerable();

            if (estatus != "Todos") listado = listado.Where(x => x.Estatus == estatus);

            // 2. Creamos el archivo Excel en memoria
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Solicitudes");

                // Encabezados
                worksheet.Cell(1, 1).Value = "Folio";
                worksheet.Cell(1, 2).Value = "Fecha";
                worksheet.Cell(1, 3).Value = "Matrícula";
                worksheet.Cell(1, 4).Value = "Nombre";
                worksheet.Cell(1, 5).Value = "Trámite";
                worksheet.Cell(1, 6).Value = "Estado";

                // Llenar datos
                int row = 2;
                foreach (var item in listado)
                {
                    worksheet.Cell(row, 1).Value = item.Id;
                    worksheet.Cell(row, 2).Value = item.Fecha.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = item.Matricula;
                    worksheet.Cell(row, 4).Value = item.Nombre;
                    worksheet.Cell(row, 5).Value = item.Tipo;
                    worksheet.Cell(row, 6).Value = item.Estatus;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileName = $"Solicitudes_{estatus}_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}

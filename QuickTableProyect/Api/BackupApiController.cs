using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using QuickTableProyect.Aplicacion;
using System;
using System.Linq;

namespace QuickTableProyect.Api
{
    [ApiController]
    [Route("api/backup")]
    public class BackupApiController : ControllerBase
    {
        private readonly BackupService _backupService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BackupApiController(BackupService backupService, IHttpContextAccessor httpContextAccessor)
        {
            _backupService = backupService;
            _httpContextAccessor = httpContextAccessor;
        }

        private (int EmpleadoId, string NombreEmpleado) ObtenerDatosEmpleado()
        {
            var session = _httpContextAccessor.HttpContext.Session;

            var empleadoIdStr = session.GetString("Id");
            var nombreEmpleado = session.GetString("Nombre");

            if (string.IsNullOrEmpty(empleadoIdStr) || string.IsNullOrEmpty(nombreEmpleado))
            {
                return (1, "Usuario TI");
            }

            if (!int.TryParse(empleadoIdStr, out int empleadoId))
            {
                return (1, nombreEmpleado ?? "Usuario TI");
            }

            return (empleadoId, nombreEmpleado);
        }

        [HttpPost("crear")]
        public IActionResult CrearBackup([FromBody] CrearBackupRequest request)
        {
            try
            {
                var (empleadoId, nombreEmpleado) = ObtenerDatosEmpleado();

                var observaciones = string.IsNullOrWhiteSpace(request.Observaciones)
                    ? "Backup creado correctamente"
                    : request.Observaciones;

                var resultado = _backupService.CrearBackupCompleto(empleadoId, nombreEmpleado, observaciones);

                if (resultado.Exito)
                {
                    return Ok(new
                    {
                        success = true,
                        message = resultado.Mensaje,
                        backupId = resultado.BackupId
                    });
                }
                else
                {
                    return Ok(new { success = false, message = resultado.Mensaje });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("listar")]
        public IActionResult ListarBackups()
        {
            try
            {
                var backups = _backupService.ObtenerHistorialBackup();
                return Ok(backups);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost("restaurar")]
        public IActionResult RestaurarBackup([FromBody] RestaurarBackupRequest request)
        {
            try
            {
                var (empleadoId, nombreEmpleado) = ObtenerDatosEmpleado();

                var resultado = _backupService.RestaurarBackup(request.BackupId, empleadoId, nombreEmpleado);

                if (resultado.Exito)
                {
                    return Ok(new { success = true, message = resultado.Mensaje, redirect = "/Login" });
                }
                else
                {
                    return Ok(new { success = false, message = resultado.Mensaje });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpDelete("eliminar/{id}")]
        public IActionResult EliminarBackup(int id)
        {
            try
            {
                var resultado = _backupService.EliminarBackup(id);

                if (resultado.Exito)
                {
                    return Ok(new { success = true, message = resultado.Mensaje });
                }
                else
                {
                    return Ok(new { success = false, message = resultado.Mensaje });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("espacio-disponible")]
        public IActionResult EspacioDisponible()
        {
            try
            {
                var espacioMB = _backupService.ObtenerEspacioDisponibleMB();
                return Ok(new { espacioDisponibleMB = espacioMB });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("descargar/{id}")]
        public IActionResult DescargarBackup(int id)
        {
            try
            {
                var backup = _backupService.ObtenerHistorialBackup()
                    .FirstOrDefault(b => b.Id == id);

                if (backup == null)
                {
                    return NotFound(new { success = false, message = "Backup no encontrado" });
                }

                if (!System.IO.File.Exists(backup.RutaArchivo))
                {
                    return NotFound(new { success = false, message = "Archivo no encontrado" });
                }

                var fileBytes = System.IO.File.ReadAllBytes(backup.RutaArchivo);
                return File(fileBytes, "application/octet-stream", backup.NombreArchivo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }

    public class CrearBackupRequest
    {
        public string NombreEmpleado { get; set; }
        public string Observaciones { get; set; }
    }

    public class RestaurarBackupRequest
    {
        public int BackupId { get; set; }
        public string NombreEmpleado { get; set; }
    }
}
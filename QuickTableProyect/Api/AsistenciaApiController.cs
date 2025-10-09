using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Persistencia.Datos;
using System;

namespace QuickTableProyect.Interface.Api
{
    [ApiController]
    [Route("api/asistencia")] 

    public class AsistenciaApiController : ControllerBase
    {
        [HttpPost("marcar-salida")]
        public IActionResult MarcarSalida([FromBody] MarcarSalidaRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Uid))
                {
                    return BadRequest(new { success = false, message = "UID de tarjeta requerido" });
                }

                // Crear contexto y servicio localmente - se liberará automáticamente
                using (var ctx = new SistemaQuickTableContext())
                {
                    var asistenciaService = new AsistenciaService(ctx);
                    var resultado = asistenciaService.MarcarSalidaConTarjeta(request.Uid);

                    if (resultado.Exito)
                    {
                        return Ok(new
                        {
                            success = true,
                            message = resultado.Mensaje,
                            nombre = resultado.NombreEmpleado,
                            rol = resultado.RolEmpleado,
                            horaIngreso = resultado.HoraIngreso,
                            horaSalida = resultado.HoraSalida,
                            tiempoTrabajado = resultado.TiempoTrabajado
                        });
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = resultado.Mensaje });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
            }
        }
    }

    public class MarcarSalidaRequest
    {
        public string Uid { get; set; }
    }
}

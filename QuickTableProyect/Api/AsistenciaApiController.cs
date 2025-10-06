using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity; // IMPORTANTE: EF6, no EF Core
using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using QuickTableProyect.Aplicacion;

namespace QuickTableProyect.Interface.Api
{
    [ApiController]
    [Route("api/asistencia")]
    public class AsistenciaApiController : ControllerBase
    {
        private readonly SistemaQuickTableContext ctx = new SistemaQuickTableContext();
        private readonly RegistroSesionService registroSesionService;
        private readonly CryptoService cryptoService;

        public AsistenciaApiController()
        {
            registroSesionService = new RegistroSesionService(ctx);
            cryptoService = new CryptoService();
        }

        // API para marcar salida con tarjeta NFC
        [HttpPost("marcar-salida")]
        public IActionResult MarcarSalida([FromBody] MarcarSalidaRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Uid))
                {
                    return BadRequest(new { success = false, message = "UID de tarjeta requerido" });
                }

                // Desencriptar el UID
                string uidDesencriptado;
                try
                {
                    uidDesencriptado = cryptoService.DesencriptarUID(request.Uid);
                }
                catch
                {
                    // Si falla la desencriptación, intentar usar el UID tal como viene
                    uidDesencriptado = request.Uid;
                }

                // Buscar empleado por UID de tarjeta
                var empleado = ctx.Empleados
                    .FirstOrDefault(e => e.TarjetaUID == uidDesencriptado && e.Activo);

                if (empleado == null)
                {
                    return NotFound(new { success = false, message = "Tarjeta no reconocida o empleado inactivo" });
                }

                // Buscar registro de sesión activo (sin FechaHoraDesconexion)
                var registroActivo = ctx.RegistroSesiones
                    .Where(r => r.EmpleadoId == empleado.Id && r.FechaHoraDesconexion == null)
                    .OrderByDescending(r => r.FechaHoraConexion)
                    .FirstOrDefault();

                if (registroActivo == null)
                {
                    return NotFound(new { success = false, message = "No hay sesión activa para este empleado" });
                }

                // Marcar salida
                var fechaSalida = DateTime.Now;
                registroActivo.FechaHoraDesconexion = fechaSalida.ToString("yyyy-MM-dd HH:mm:ss");
                registroActivo.MarcoTarjetaSalida = true; // Indicar que salió con tarjeta

                ctx.SaveChanges();

                // Calcular tiempo trabajado
                var tiempoTrabajado = fechaSalida - registroActivo.FechaHoraConexion;
                var tiempoFormateado = $"{(int)tiempoTrabajado.TotalHours:D2}:{tiempoTrabajado.Minutes:D2}";

                return Ok(new
                {
                    success = true,
                    message = "Salida registrada exitosamente",
                    nombre = empleado.Nombre,
                    rol = empleado.Rol,
                    empleadoId = empleado.Id,
                    horaIngreso = registroActivo.FechaHoraConexion.ToString("HH:mm"),
                    horaSalida = fechaSalida.ToString("HH:mm"),
                    tiempoTrabajado = tiempoFormateado
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // API para consultar estado de asistencia
        [HttpGet("estado/{empleadoId}")]
        public IActionResult EstadoAsistencia(int empleadoId)
        {
            try
            {
                var empleado = ctx.Empleados.Find(empleadoId);
                if (empleado == null)
                {
                    return NotFound(new { success = false, message = "Empleado no encontrado" });
                }

                var registroActivo = ctx.RegistroSesiones
                    .Where(r => r.EmpleadoId == empleadoId && r.FechaHoraDesconexion == null)
                    .OrderByDescending(r => r.FechaHoraConexion)
                    .FirstOrDefault();

                return Ok(new
                {
                    success = true,
                    empleadoId = empleado.Id,
                    nombre = empleado.Nombre,
                    sesionActiva = registroActivo != null,
                    horaIngreso = registroActivo?.FechaHoraConexion.ToString("HH:mm"),
                    tiempoParcial = registroActivo != null ?
                        $"{(int)(DateTime.Now - registroActivo.FechaHoraConexion).TotalHours:D2}:{(DateTime.Now - registroActivo.FechaHoraConexion).Minutes:D2}" :
                        null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }

    // Clase para el request de marcar salida
    public class MarcarSalidaRequest
    {
        public string Uid { get; set; }
    }
}

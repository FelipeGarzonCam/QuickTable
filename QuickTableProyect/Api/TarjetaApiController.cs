using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Data.Entity; 
using System.Linq;

namespace QuickTableProyect.Interface.Api
{
    [ApiController]
    [Route("api/tarjeta")]
    public class TarjetaApiController : ControllerBase
    {
        private readonly SistemaQuickTableContext ctx = new();
        private readonly CryptoService cryptoService; 

        
        public TarjetaApiController()
        {
            cryptoService = new CryptoService();
        }

        // 1. devuelve UIDs pendientes de grabar
        [HttpGet("pendientes")]
        public IActionResult Pendientes()
        {
            try
            {
                var lista = ctx.TarjetasRC
                    .Where(t => !t.Activa)
                    .Select(t => t.Uid)
                    .ToList();

                return Ok(lista);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // 2. Raspberry confirma grabación        
        [HttpPost("confirmar")]
        public IActionResult Confirmar([FromForm] string uidLeido)
        {
            try
            {
                var tarjetaPendiente = ctx.TarjetasRC
                    .Where(t => !t.Activa)
                    .OrderByDescending(t => t.FechaAsignacion)
                    .FirstOrDefault();

                if (tarjetaPendiente == null)
                    return NotFound(new { message = "No hay tarjetas pendientes" });

                tarjetaPendiente.Activa = true;
                tarjetaPendiente.UidFisico = uidLeido;  // ← GUARDAR UID físico
                ctx.SaveChanges();

                return Ok(new { message = "Tarjeta activada correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // 3. Endpoint para polling desde el navegador
        [HttpGet("estado")]
        public IActionResult Estado(string uid)
        {
            try
            {
                if (string.IsNullOrEmpty(uid))
                    return BadRequest(new { error = "UID requerido" });

                bool activa = ctx.TarjetasRC.Any(t => t.Uid == uid && t.Activa);
                return Ok(activa);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // 4. Validar código de sesión desde Raspberry Pi
        [HttpPost("validar-sesion")]
        public IActionResult ValidarSesion([FromForm] string sessionCode)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionCode) || sessionCode.Length != 6 || !sessionCode.All(char.IsDigit))
                    return Ok(new { valid = false, message = "Código inválido" });

                // BUSCAR tarjeta con el código específico
                var tarjetaPendiente = ctx.TarjetasRC
                    .Include(t => t.Empleado)
                    .FirstOrDefault(t => !t.Activa &&
                                       t.Empleado.Rol == "Admin" &&
                                       t.CodigoSesion == sessionCode);

                if (tarjetaPendiente != null)
                {
                    return Ok(new
                    {
                        valid = true,
                        role = "TI",
                        uid = tarjetaPendiente.Uid,
                        empleadoId = tarjetaPendiente.EmpleadoId,
                        adminNombre = tarjetaPendiente.Empleado?.Nombre ?? "Sin nombre",
                        fechaAsignacion = tarjetaPendiente.FechaAsignacion
                    });
                }

                return NotFound(new { message = "Código de TI inválido o sin tarjetas pendientes" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { valid = false, error = ex.Message });
            }
        }

        // 5. Obtener información específica de una sesión
        [HttpGet("sesion-info")]
        public IActionResult SesionInfo(string sessionCode)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionCode) || sessionCode.Length != 6)
                    return Ok(new { valid = false, message = "Código inválido" });

                // Buscar tarjeta pendiente más reciente
                var tarjetaPendiente = ctx.TarjetasRC
                    .Include(t => t.Empleado) // EF6 syntax
                    .Where(t => !t.Activa)
                    .OrderByDescending(t => t.FechaAsignacion)
                    .FirstOrDefault();

                if (tarjetaPendiente != null)
                {
                    return Ok(new
                    {
                        valid = true,
                        role = "TI",
                        uid = tarjetaPendiente.Uid,
                        empleadoId = tarjetaPendiente.EmpleadoId,
                        adminNombre = tarjetaPendiente.Empleado?.Nombre ?? "Sin nombre",
                        fechaAsignacion = tarjetaPendiente.FechaAsignacion?.ToString("dd/MM/yyyy HH:mm")
                    });
                }

                return Ok(new { valid = false, message = "No hay tarjetas pendientes" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { valid = false, error = ex.Message });
            }
        }

        // 6. Obtener información de sesión activa 
        [HttpGet("sesion-activa")]
        public IActionResult SesionActiva(string sessionCode)
        {
            try
            {
                // Validar código
                if (string.IsNullOrEmpty(sessionCode) || sessionCode.Length != 6 || !sessionCode.All(char.IsDigit))
                    return Ok(new { valid = false });

                var tarjetaPendiente = ctx.TarjetasRC
                    .Where(t => !t.Activa)
                    .OrderByDescending(t => t.FechaAsignacion)
                    .FirstOrDefault();

                if (tarjetaPendiente != null)
                {
                    return Ok(new
                    {
                        valid = true,
                        role = "TI",
                        uid = tarjetaPendiente.Uid,
                        empleadoId = tarjetaPendiente.EmpleadoId
                    });
                }

                return Ok(new { valid = false });
            }
            catch (Exception ex)
            {
                return BadRequest(new { valid = false, error = ex.Message });
            }
        }

        // 7. Endpoint para Admin 2FA
        [HttpPost("validar-sesion-admin")]
        public IActionResult ValidarSesionAdmin([FromForm] string sessionCode)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionCode) || sessionCode.Length != 6 || !sessionCode.All(char.IsDigit))
                    return Ok(new { valid = false, message = "Código inválido" });

                // Para Admin, buscar código 2FA activo
                var codigo2FA = ctx.Codigos2FA
                    .Include(c => c.Empleado) // EF6 syntax
                    .Where(c => !c.Confirmado && c.Expiracion > DateTime.Now)
                    .FirstOrDefault();

                if (codigo2FA != null && codigo2FA.Empleado?.Rol == "Admin")
                {
                    return Ok(new
                    {
                        valid = true,
                        role = "Admin",
                        navId = codigo2FA.NavegadorId,
                        empleadoId = codigo2FA.EmpleadoId,
                        adminNombre = codigo2FA.Empleado?.Nombre ?? "Admin"
                    });
                }

                return Ok(new { valid = false, message = "No hay sesiones de Admin pendientes" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { valid = false, error = ex.Message });
            }
        }

        // 8. Test de conectividad
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                status = "OK",
                timestamp = DateTime.Now,
                message = "TarjetaApiController funcionando correctamente"
            });
        }

        // 9. Asignar tarjeta a empleado
        [HttpPost("asignar-empleado")]
        public IActionResult AsignarTarjetaEmpleado([FromBody] AsignarEmpleadoRequest request)
        {
            try
            {
                if (request == null || request.EmpleadoId <= 0 || string.IsNullOrEmpty(request.Uid))
                {
                    return BadRequest(new { success = false, message = "Datos inválidos" });
                }

                var empleado = ctx.Empleados.Find(request.EmpleadoId);
                if (empleado == null)
                {
                    return BadRequest(new { success = false, message = "Empleado no encontrado" });
                }

                //  Usar el UID tal como viene 
                var uidParaGuardar = request.Uid;

                // Verificar que la tarjeta no esté ya asignada a otro empleado
                var empleadoExistente = ctx.Empleados
                    .FirstOrDefault(e => e.TarjetaUID == uidParaGuardar && e.Id != request.EmpleadoId);

                if (empleadoExistente != null)
                {
                    return BadRequest(new { success = false, message = $"Tarjeta ya asignada a {empleadoExistente.Nombre}" });
                }

                // Asignar tarjeta al empleado
                empleado.TarjetaUID = uidParaGuardar;
                ctx.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = $"Tarjeta asignada a {empleado.Nombre}",
                    nombre = empleado.Nombre,
                    rol = empleado.Rol
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // 10.  Validar código de sesión para empleados
        [HttpPost("validar-sesion-empleado")]
        public IActionResult ValidarSesionEmpleado([FromForm] string sessionCode)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionCode) || sessionCode.Length != 6 || !sessionCode.All(char.IsDigit))
                {
                    return Ok(new { valid = false, message = "Código inválido" });
                }

                // BUSCAR código de sesión en Codigos2FA
                var codigoSesion = ctx.Codigos2FA
                    .Include(c => c.Empleado)
                    .FirstOrDefault(c => c.Codigo == sessionCode &&
                                       c.Expiracion > DateTime.Now &&
                                       c.EsParaTarjetaEmpleado == true);

                if (codigoSesion != null)
                {
                    return Ok(new
                    {
                        valid = true,
                        tipo = "tarjeta-empleado",
                        empleadoId = codigoSesion.EmpleadoId,
                        nombre = codigoSesion.Empleado?.Nombre ?? "Sin nombre",
                        rolEmpleado = codigoSesion.Empleado?.Rol ?? "Sin rol"
                    });
                }

                return Ok(new { valid = false, message = "Código de empleado inválido o expirado" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { valid = false, error = ex.Message });
            }
        }



        // Health check endpoint
        [HttpGet("health")]
        public IActionResult Health()
        {
            try
            {
                // Comprobar conexión a base de datos
                var count = ctx.Empleados.Count();

                return Ok(new
                {
                    status = "OK",
                    timestamp = DateTime.Now,
                    database = "Connected",
                    message = "Sistema funcionando correctamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "ERROR",
                    timestamp = DateTime.Now,
                    database = "Disconnected",
                    error = ex.Message
                });
            }
        }
        
        public class AsignarEmpleadoRequest
        {
            public int EmpleadoId { get; set; }  
            public string Uid { get; set; }     
        }

        // AGREGAR este método al TarjetaApiController existente
        [HttpPost("marcar-salida-empleado")]
        public IActionResult MarcarSalidaEmpleado([FromForm] string tarjetaUID)
        {
            try
            {
                if (string.IsNullOrEmpty(tarjetaUID))
                    return BadRequest(new { message = "UID de tarjeta requerido" });

                // BÚSQUEDA UNIFICADA: Empleados normales O admins con TarjetasRC
                var empleado = ctx.Empleados
                    .Where(e =>
                        e.TarjetaUID == tarjetaUID || // Empleados normales
                        ctx.TarjetasRC.Any(t =>
                            t.EmpleadoId == e.Id &&
                            t.Activa &&
                            t.UidFisico == tarjetaUID)) // Admins
                    .FirstOrDefault();

                if (empleado == null)
                    return BadRequest(new { message = "Tarjeta no autorizada" });

               
                var sesionActiva = ctx.RegistroSesiones
                    .Where(r => r.EmpleadoId == empleado.Id && r.FechaHoraDesconexion == null)
                    .OrderByDescending(r => r.FechaHoraConexion)
                    .FirstOrDefault();

                if (sesionActiva != null)
                {
                    sesionActiva.FechaHoraDesconexion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    sesionActiva.MarcoTarjetaSalida = true;
                    ctx.SaveChanges();

                    return Ok(new
                    {
                        empleado = empleado.Nombre,
                        accion = "Salida registrada",
                        tipo = "salida"
                    });
                }
                else
                {
                    return BadRequest(new { message = "No hay sesión activa para marcar salida" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error: {ex.Message}" });
            }
        }

    }


}

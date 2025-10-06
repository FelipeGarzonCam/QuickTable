using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Data.Entity; // IMPORTANTE: EF6, no EF Core
using System.Linq;

namespace QuickTableProyect.Interface.Api
{
    [ApiController]
    [Route("api/tarjeta")]
    public class TarjetaApiController : ControllerBase
    {
        private readonly SistemaQuickTableContext ctx = new();

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

        // 6. Obtener información de sesión activa (simplificado)
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

        // 7. NUEVO: Endpoint para Admin 2FA
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

        // 8. NUEVO: Test de conectividad
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
        // AGREGAR estos métodos al final del TarjetaApiController existente:

        // 9. NUEVO: Asignar tarjeta a empleado (no admin)
        [HttpPost("asignar-empleado")]
        public IActionResult AsignarTarjetaEmpleado([FromBody] AsignarEmpleadoRequest request)
        {
            try
            {
                if (request == null || request.empleadoId <= 0 || string.IsNullOrEmpty(request.uid))
                {
                    return BadRequest(new { success = false, message = "Datos inválidos" });
                }

                // Buscar el código de sesión temporal para empleado
                var codigoSesion = ctx.Codigos2FA
                    .FirstOrDefault(c => c.EmpleadoId == request.empleadoId &&
                                        c.Expiracion > DateTime.Now &&
                                        c.EsParaTarjetaEmpleado == true);

                if (codigoSesion == null)
                {
                    return BadRequest(new { success = false, message = "Código de sesión expirado o inválido" });
                }

                var empleado = ctx.Empleados.Find(request.empleadoId);
                if (empleado == null)
                {
                    return BadRequest(new { success = false, message = "Empleado no encontrado" });
                }

                // Encriptar el UID usando el CryptoService
                string uidEncriptado = CryptoService.Encrypt(request.uid);

                // Verificar que la tarjeta no esté ya asignada a otro empleado
                var empleadoExistente = ctx.Empleados
                    .FirstOrDefault(e => e.TarjetaUID == uidEncriptado && e.Id != request.empleadoId);

                if (empleadoExistente != null)
                {
                    return BadRequest(new { success = false, message = $"Tarjeta ya asignada a {empleadoExistente.Nombre}" });
                }

                // Asignar tarjeta al empleado
                empleado.TarjetaUID = uidEncriptado;

                // Eliminar el código temporal usado
                ctx.Codigos2FA.Remove(codigoSesion);

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

        // 10. NUEVO: Validar código de sesión para empleados
        [HttpPost("validar-sesion-empleado")]
        public IActionResult ValidarSesionEmpleado([FromForm] string sessionCode)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionCode) || sessionCode.Length != 6 || !sessionCode.All(char.IsDigit))
                {
                    return Ok(new { valid = false, message = "Código inválido" });
                }

                // Para empleados, buscar código de sesión para tarjeta
                var codigoSesion = ctx.Codigos2FA
                    .Include(c => c.Empleado) // EF6 syntax
                    .Where(c => c.Codigo == sessionCode &&
                               c.Expiracion > DateTime.Now &&
                               c.EsParaTarjetaEmpleado == true)
                    .FirstOrDefault();

                if (codigoSesion != null)
                {
                    return Ok(new
                    {
                        valid = true,
                        role = "Empleado",
                        empleadoId = codigoSesion.EmpleadoId,
                        nombre = codigoSesion.Empleado?.Nombre ?? "Sin nombre",
                        rolEmpleado = codigoSesion.Empleado?.Rol ?? "Sin rol",
                        tipo = "tarjeta-empleado"
                    });
                }

                return Ok(new { valid = false, message = "Código de empleado inválido o expirado" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { valid = false, error = ex.Message });
            }
        }

        // Clase para recibir la petición de asignar tarjeta a empleado
        public class AsignarEmpleadoRequest
        {
            public int empleadoId { get; set; }
            public string uid { get; set; }
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


    }
}

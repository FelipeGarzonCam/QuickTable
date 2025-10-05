using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System.Data.Entity;
using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;


namespace QuickTableProyect.Interface
{
    // Clases para recibir JSON correctamente (sin JsonElement)
    public class AdminRequest
    {
        public int adminId { get; set; }
        public string nuevaClave { get; set; } = "";
    }

    public class AdminIdRequest
    {
        public int adminId { get; set; }
    }

    public class TIController : Controller
    {
        private readonly SistemaQuickTableContext _ctx = new();

        public override void OnActionExecuting(ActionExecutingContext ctx)
        {
            if (HttpContext.Session.GetString("Rol") != "TI")
                ctx.Result = RedirectToAction("Index", "Login");
            base.OnActionExecuting(ctx);
        }
        public class SesionTiDto
        {
            public string uid { get; set; }
            public string codigoSesion { get; set; }
        }

        /* ---------- LISTADO ---------- */
        public IActionResult Index()
        {
            var tarjetas = _ctx.TarjetasRC.Include(t => t.Empleado)
                                          .OrderBy(t => t.Id)
                                          .ToList();

            return View(tarjetas);
        }

        /* ---------- NUEVO ADMIN + TARJETA ---------- */
        [HttpGet]
        public IActionResult CrearAdministrador() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CrearAdministrador(string nombre, string contrasena)
        {
            try
            {
                // LIMPIAR códigos 2FA vencidos antes de crear
                var vencidos = _ctx.Codigos2FA.Where(c => c.Expiracion < DateTime.Now).ToList();
                _ctx.Codigos2FA.RemoveRange(vencidos);
                // 1. Crear el usuario rol Admin
                var admin = new Empleado
                {
                    Nombre = nombre,
                    Contrasena = contrasena,
                    Rol = "Admin"
                };
                _ctx.Empleados.Add(admin);
                _ctx.SaveChanges();

                // 2. Generar UID y crear tarjeta NO ACTIVA
                string uid = Guid.NewGuid().ToString("N")[..8].ToUpper();

                _ctx.TarjetasRC.Add(new TarjetaRC
                {
                    Uid = uid,
                    EmpleadoId = admin.Id,
                    FechaAsignacion = DateTime.Now,
                    Activa = false  // IMPORTANTE: Inicia como no activa
                });
                _ctx.SaveChanges();

                TempData["Ok"] = $"Administrador {nombre} creado correctamente. Recuerde Asignar Una Tarjeta";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear administrador: {ex.Message}";
                return View();
            }
        }

        /* ---------- MÉTODOS JSON PARA MODALES (CORREGIDOS) ---------- */
        [HttpPost]
        public JsonResult CambiarClaveAdmin([FromBody] AdminRequest data)
        {
            try
            {
                if (data == null || string.IsNullOrEmpty(data.nuevaClave))
                    return Json(new { success = false, message = "Datos inválidos" });

                var admin = _ctx.Empleados.FirstOrDefault(e => e.Id == data.adminId && e.Rol == "Admin");
                if (admin == null)
                    return Json(new { success = false, message = "Administrador no encontrado" });

                admin.Contrasena = data.nuevaClave;
                _ctx.SaveChanges();

                return Json(new { success = true, message = "Contraseña actualizada correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult EliminarAdmin([FromBody] AdminIdRequest data)
        {
            try
            {
                if (data == null || data.adminId <= 0)
                    return Json(new { success = false, message = "ID de administrador inválido" });

                var admin = _ctx.Empleados.FirstOrDefault(e => e.Id == data.adminId && e.Rol == "Admin");
                if (admin == null)
                    return Json(new { success = false, message = "Administrador no encontrado" });

                // 1. LIMPIAR códigos 2FA del admin
                var codigos2FA = _ctx.Codigos2FA.Where(c => c.EmpleadoId == data.adminId).ToList();
                _ctx.Codigos2FA.RemoveRange(codigos2FA);

                // 2. LIMPIAR tarjeta del admin
                var tarjeta = _ctx.TarjetasRC.FirstOrDefault(t => t.EmpleadoId == data.adminId);
                if (tarjeta != null)
                {
                    _ctx.TarjetasRC.Remove(tarjeta);
                }

                // 3. ELIMINAR el empleado
                _ctx.Empleados.Remove(admin);

                _ctx.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult RegenerarTarjeta([FromBody] AdminIdRequest data)
        {
            try
            {
                if (data == null || data.adminId <= 0)
                    return Json(new { success = false, message = "ID de administrador inválido" });

                // Buscar administrador
                var admin = _ctx.Empleados.FirstOrDefault(e => e.Id == data.adminId && e.Rol == "Admin");
                if (admin == null)
                    return Json(new { success = false, message = "Administrador no encontrado" });

                // Eliminar tarjeta anterior
                var tarjetaAnterior = _ctx.TarjetasRC.FirstOrDefault(t => t.EmpleadoId == data.adminId);
                if (tarjetaAnterior != null)
                    _ctx.TarjetasRC.Remove(tarjetaAnterior);

                // Crear nueva tarjeta NO ACTIVA
                string nuevoUid = Guid.NewGuid().ToString("N")[..8].ToUpper();
                _ctx.TarjetasRC.Add(new TarjetaRC
                {
                    Uid = nuevoUid,
                    EmpleadoId = data.adminId,
                    FechaAsignacion = DateTime.Now,
                    Activa = false  // Importante: empieza inactiva hasta ser grabada
                });
                _ctx.SaveChanges();

                // Generar nuevo código de sesión para la regeneración
                string sessionCode = Math.Abs(HttpContext.Session.Id.GetHashCode()).ToString("000000")[..6];


                return Json(new
                {
                    success = true,
                    uid = nuevoUid,
                    message = "Tarjeta regenerada correctamente"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /* ---------- MÉTODO PARA OBTENER ESTADÍSTICAS (OPCIONAL) ---------- */
        [HttpGet]
        public JsonResult EstadisticasTarjetas()
        {
            try
            {
                var total = _ctx.TarjetasRC.Count();
                var activas = _ctx.TarjetasRC.Count(t => t.Activa);
                var pendientes = total - activas;

                return Json(new
                {
                    success = true,
                    total = total,
                    activas = activas,
                    pendientes = pendientes
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AsociarCodigoSesion([FromBody] SesionTiDto data)
        {
            try
            {
                // BUSCAR la tarjeta por UID y actualizarla
                var tarjeta = _ctx.TarjetasRC.FirstOrDefault(t => t.Uid == data.uid && !t.Activa);

                if (tarjeta == null)
                    return Json(new { success = false, message = "Tarjeta no encontrada o ya activa" });

                // GUARDAR el código en la tarjeta
                tarjeta.CodigoSesion = data.codigoSesion;
                _ctx.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

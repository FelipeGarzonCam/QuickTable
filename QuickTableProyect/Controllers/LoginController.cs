using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;                                // EF 6 → Include()
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;

namespace QuickTableProyect.Interface
{
    public class LoginController : Controller
    {
        private readonly EmpleadoService _empleadoService;
        private readonly RegistroSesionService _sesionService;
        private readonly SistemaQuickTableContext _ctx = new();      // EF6 DbContext
        private readonly RegistroSesionService sesionService;

        public LoginController(
            EmpleadoService empleadoService,
            RegistroSesionService sesionService)
        {
            _empleadoService = empleadoService;
            _sesionService = sesionService;
            this.sesionService = sesionService;
        }

        // ----------- GET /Login --------------
        public IActionResult Index()
        {
            string rol = HttpContext.Session.GetString("Rol");
            if (!string.IsNullOrEmpty(rol))
            {
                // CRÍTICO: Verificar si es Admin y requiere 2FA
                if (rol == "Admin")
                {
                    string empIdString = HttpContext.Session.GetString("Id");
                    if (int.TryParse(empIdString, out int empId))
                    {
                        // Verificar si tiene 2FA pendiente
                        bool tiene2FAPendiente = _ctx.Codigos2FA.Any(c =>
                            c.EmpleadoId == empId &&
                            !c.Confirmado &&
                            c.Expiracion > DateTime.Now);

                        if (tiene2FAPendiente)
                        {
                            // Forzar logout y redirigir a login
                            HttpContext.Session.Clear();
                            ViewBag.Error = "Debe completar la autenticación 2FA";
                            return View();
                        }
                    }
                }

                return rol switch
                {
                    "Admin" => RedirectToAction("Index", "Administrador"),
                    "Mesero" => RedirectToAction("Index", "Mesero"),
                    "Cocina" => RedirectToAction("Index", "Cocina"),
                    "Cajero" => RedirectToAction("Index", "Caja"),
                    "TI" => RedirectToAction("Index", "TI"),  
                    _ => RedirectToAction("Index", "Login")
                };
            }
            return View();
        }


        // ----------- POST /Login/Autenticar --------------
        [HttpPost]
        public JsonResult Autenticar(string nombre, string contrasena)
        {
            LimpiarCodigosVencidos();
            var emp = _empleadoService.ObtenerEmpleadoPorNombre(nombre);
            if (emp == null || emp.Contrasena != contrasena)
                return Json(new { success = false, message = "Nombre o contraseña incorrectos." });

            // 1) marca sesión anterior como error
            _sesionService.MarcarErroresPendientes(emp.Id);

            // 2) registra nueva conexión
            int regId = _sesionService.RegistrarConexion(emp.Id);

            // 3) guarda datos en la sesión de ASP.NET Core
            HttpContext.Session.SetString("Rol", emp.Rol);
            HttpContext.Session.SetString("Id", emp.Id.ToString());
            HttpContext.Session.SetString("Nombre", emp.Nombre);
            HttpContext.Session.SetInt32("RegistroSesionId", regId);

            // ----- segundo factor SOLO para administradores -----
            if (emp.Rol == "Admin")
            {
                string code = new Random().Next(100_000, 999_999).ToString();
                Guid navId = Guid.NewGuid();

                _ctx.Codigos2FA.Add(new Codigo2FA
                {
                    Codigo = code,
                    NavegadorId = navId,
                    EmpleadoId = emp.Id,
                    Expiracion = DateTime.Now.AddMinutes(10),
                    Confirmado = false
                });
                _ctx.SaveChanges();

                return Json(new
                {
                    success = true,
                    requiere2FA = true,
                    code,
                    nav = navId     // usado por el modal en JS
                });
            }

            // redirección normal si no requiere 2FA
            string url = emp.Rol switch
            {
                "Admin" => Url.Action("Index", "Administrador"),
                "Mesero" => Url.Action("Index", "Mesero"),
                "Cocina" => Url.Action("Index", "Cocina"),
                "Cajaero" => Url.Action("Index", "Caja"),
                "TI" => Url.Action("Index", "Ti"),
                _ => Url.Action("Index", "Login")
            };
            return Json(new { success = true, redirectUrl = url });
        }

        // ----------- POST /Login/Confirmar2FA --------------
        [HttpPost]
        public IActionResult Confirmar2FA(string navId, string uidFisico, string textoEscrito)
        {
            try
            {
                if (!Guid.TryParse(navId, out Guid navegadorId))
                    return BadRequest("ID de navegador inválido");

                // BUSCAR tarjeta que coincida con AMBOS valores
                var tarjeta = _ctx.TarjetasRC
                    .Include(t => t.Empleado)
                    .FirstOrDefault(t => t.Activa &&
                                       t.Empleado.Rol == "Admin" &&
                                       t.UidFisico == uidFisico &&         // UID del chip
                                       t.Uid == textoEscrito.Trim());      // UID que escribimos

                if (tarjeta == null)
                {                    
                    return BadRequest("Tarjeta no autorizada");
                }

                // Confirmar 2FA
                var codigo = _ctx.Codigos2FA.FirstOrDefault(c => c.NavegadorId == navegadorId && !c.Confirmado);
                if (codigo != null)
                {
                    codigo.Confirmado = true;
                    _ctx.SaveChanges();                   
                    return Ok("2FA confirmado");
                }

                return BadRequest("Código 2FA no encontrado");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }


        // ----------- GET /Login/Check2FA --------------
        [HttpGet]
        public JsonResult Check2FA(Guid navId)
        {
            bool ok = _ctx.Codigos2FA.Any(c => c.NavegadorId == navId && c.Confirmado);
            return Json(ok);                                      // JsonRequestBehavior ya no se usa en Core :contentReference[oaicite:5]{index=5}
        }

        // ----------- utilitario: ¿tiene 2FA pendiente? --------------
        private bool Requiere2FA(int empId) =>
            _ctx.Codigos2FA.Any(c => c.EmpleadoId == empId &&
                                     !c.Confirmado &&
                                     c.Expiracion > DateTime.Now);

        // ----------- GET /Login/Logout --------------
        public IActionResult Logout()
        {
            int? regId = HttpContext.Session.GetInt32("RegistroSesionId");
            if (regId.HasValue) _sesionService.RegistrarDesconexion(regId.Value);

            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
        private void LimpiarCodigosVencidos()
        {
            var vencidos = _ctx.Codigos2FA.Where(c => c.Expiracion < DateTime.Now).ToList();
            _ctx.Codigos2FA.RemoveRange(vencidos);
            _ctx.SaveChanges();
            
        }
        // Agregar este método al LoginController existente
        // QuickTableProyect/Controllers/LoginController.cs

        // QuickTableProyect/Controllers/LoginController.cs
        // Agregar este método al LoginController existente

        // QuickTableProyect/Controllers/LoginController.cs
        // Reemplazar el método FinalizarDia existente con este:

        [HttpPost]
        public JsonResult FinalizarDia()
        {
            try
            {
                var empleadoIdString = HttpContext.Session.GetString("Id");

                if (string.IsNullOrEmpty(empleadoIdString))
                {
                    return Json(new { success = false, message = "No hay sesión activa" });
                }

                if (!int.TryParse(empleadoIdString, out int empleadoId))
                {
                    return Json(new { success = false, message = "ID de empleado inválido" });
                }

                // Finalizar día laboral usando sesionService (NO registroSesionService)
                sesionService.FinalizarDiaLaboral(empleadoId);

                // Limpiar la sesión
                HttpContext.Session.Clear();

                return Json(new { success = true, message = "Día laboral finalizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }



    }
}

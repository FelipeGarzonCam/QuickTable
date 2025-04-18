using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using Microsoft.AspNetCore.Http;

namespace QuickTableProyect.Interface
{
    public class LoginController : Controller
    {
        private readonly EmpleadoService _empleadoService;
        private readonly RegistroSesionService _sesionService;

        public LoginController(EmpleadoService empleadoService, RegistroSesionService sesionService)
        {
            _empleadoService = empleadoService;
            _sesionService = sesionService;
        }

        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (!string.IsNullOrEmpty(rol))
            {
                // Redirige al usuario a su vista según el rol
                switch (rol)
                {
                    case "Admin":
                        return RedirectToAction("Index", "Administrador");
                    case "Mesero":
                        return RedirectToAction("Index", "Mesero");
                    case "Cocina":
                        return RedirectToAction("Index", "Cocina");
                    case "Caja":
                        return RedirectToAction("Index", "Caja");
                    default:
                        return RedirectToAction("Index", "Login");
                }
            }
            return View();
        }

        [HttpPost]
        public JsonResult Autenticar(string nombre, string contrasena)
        {
            var empleado = _empleadoService.ObtenerEmpleadoPorNombre(nombre);
            if (empleado != null && empleado.Contrasena == contrasena)
            {
                // 1) Marca como error las sesiones previas
                _sesionService.MarcarErroresPendientes(empleado.Id);

                // 2) Registra la nueva conexión
                int registroId = _sesionService.RegistrarConexion(empleado.Id);

                // 3) Guarda en sesión ASP.NET
                HttpContext.Session.SetString("Rol", empleado.Rol);
                HttpContext.Session.SetString("Id", empleado.Id.ToString());
                HttpContext.Session.SetString("Nombre", empleado.Nombre);
                HttpContext.Session.SetInt32("RegistroSesionId", registroId);

                string redirectUrl = empleado.Rol switch
                {
                    "Admin" => Url.Action("Index", "Administrador"),
                    "Mesero" => Url.Action("Index", "Mesero"),
                    "Cocina" => Url.Action("Index", "Cocina"),
                    "Cajero" => Url.Action("Index", "Caja"),
                    _ => Url.Action("Index", "Login")
                };

                return Json(new { success = true, redirectUrl });
            }
            return Json(new { success = false, message = "Nombre o contraseña incorrectos." });
        }

        public IActionResult Logout()
        {
            // Desconexión normal
            var regId = HttpContext.Session.GetInt32("RegistroSesionId");
            if (regId.HasValue)
            {
                _sesionService.RegistrarDesconexion(regId.Value);
            }

            // Limpia sesión y redirige al login
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}

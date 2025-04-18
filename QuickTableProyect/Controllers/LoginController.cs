using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using Microsoft.AspNetCore.Http;

namespace QuickTableProyect.Interface
{
    public class LoginController : Controller
    {
        private readonly EmpleadoService _empleadoService;
        private readonly RegistroSesionService _registroSesionService;

        public LoginController(EmpleadoService empleadoService,RegistroSesionService registroSesionService)        
        {
            _empleadoService = empleadoService;
            _registroSesionService = registroSesionService;
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
            // Obtiene el empleado por nombre
            var empleado = _empleadoService.ObtenerEmpleadoPorNombre(nombre); // Verifica que el método exista en EmpleadoService

            if (empleado != null && empleado.Contrasena == contrasena)
            {
                // Almacena en sesión Rol, Id y Nombre del empleado
                HttpContext.Session.SetString("Rol", empleado.Rol);
                HttpContext.Session.SetString("Id", empleado.Id.ToString());
                HttpContext.Session.SetString("Nombre", empleado.Nombre);

                // registro la conexión y guardo el registroId en sesión
                int registroId = _registroSesionService.RegistrarConexion(empleado.Id);
                HttpContext.Session.SetInt32("RegistroSesionId", registroId);

                // Define la URL de redirección según el rol del empleado
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
            // antes de limpiar, cierro la sesión en BD
            var regId = HttpContext.Session.GetInt32("RegistroSesionId");
            if (regId.HasValue)
                _registroSesionService.RegistrarDesconexion(regId.Value);

            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using Microsoft.AspNetCore.Http;

namespace QuickTableProyect.Interface
{
    public class LoginController : Controller
    {
        private readonly EmpleadoService _empleadoService;

        public LoginController(EmpleadoService empleadoService)
        {
            _empleadoService = empleadoService;
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
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}

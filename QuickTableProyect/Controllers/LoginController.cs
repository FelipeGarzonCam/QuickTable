using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;


namespace QuickTableProyect.Interface
{
    public class LoginController : Controller
    {
        private readonly EmpleadoService _empleadoService;
        private readonly RegistroSesionService _sesionService;
        private readonly ISuperAdminService _superAdminService;

        public LoginController(EmpleadoService empleadoService, RegistroSesionService sesionService, ISuperAdminService superAdminService)
        {
            _empleadoService = empleadoService;
            _sesionService = sesionService;
            _superAdminService = superAdminService;
        }

        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (!string.IsNullOrEmpty(rol))
            {
                // Redirige al usuario a su vista según el rol
                switch (rol)
                {
                    case "SuperAdmin":
                         return RedirectToAction("Index", "SuperAdmin");
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
        public async Task<JsonResult> Autenticar(string nombre, string contrasena)
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

                // Si no es empleado, pruebo con SuperAdmin
                if (redirectUrl == Url.Action("Index", "Login"))
                {
                    var superAdmin = await _superAdminService.AuthenticateAsync(nombre, contrasena);
                    if (superAdmin != null)
                    {
                        HttpContext.Session.SetString("Rol", "SuperAdmin");
                        HttpContext.Session.SetString("Id", superAdmin.Id.ToString());
                        HttpContext.Session.SetString("Nombre", superAdmin.Nombre);
                        var redirectUrlSA = Url.Action("Index", "SuperAdmin");
                        return Json(new { success = true, redirectUrl = redirectUrlSA });
                    }
                }

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

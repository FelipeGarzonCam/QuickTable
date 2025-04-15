using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using Microsoft.AspNetCore.Http;
using QuickTableProyect.Dominio;
using Microsoft.AspNetCore.SignalR;
using QuickTableProyect.Interface.Hubs;

namespace QuickTableProyect.Interface
{
    public class LoginController : Controller
    {
        private readonly EmpleadoService _empleadoService;
        private readonly IHubContext<ComunicacionHub> _hubContext;

        public LoginController(EmpleadoService empleadoService, IHubContext<ComunicacionHub> hubContext)
        {
            _empleadoService = empleadoService;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (!string.IsNullOrEmpty(rol))
            {                
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
        public JsonResult Autenticar(string nombre, string contrasena, string clientId)
        {
            var empleado = _empleadoService.ObtenerEmpleadoPorNombre(nombre);

            if (empleado != null && empleado.Contrasena == contrasena)
            {
                HttpContext.Session.SetString("Rol", empleado.Rol);
                HttpContext.Session.SetString("Id", empleado.Id.ToString());
                HttpContext.Session.SetString("Nombre", empleado.Nombre);

                // Almacenar el estado de inicio de sesión
                SharedState.LoginStatus[clientId] = true;

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
            else
            {
                // Almacenar el estado de inicio de sesión fallido
                SharedState.LoginStatus[clientId] = false;
            }

            return Json(new { success = false, message = "Nombre o contraseña incorrectos." });
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
        //NODE

        [HttpGet]
        public async Task<IActionResult> SeleccionarCampoUsuario(string clientId)
        {
            SharedState.CampoSeleccionado = "Usuario";
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarCampos", new { campoSeleccionado = "Usuario" });
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> SeleccionarCampoContrasena(string clientId)
        {
            SharedState.CampoSeleccionado = "Contrasena";
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarCampos", new { campoSeleccionado = "Contrasena" });
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> BorrarCampoUsuario(string clientId)
        {
            SharedState.UsuarioInput = "";
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarCampos", new { usuarioInput = SharedState.UsuarioInput });
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> BorrarCampoContrasena(string clientId)
        {
            SharedState.ContrasenaInput = "";
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarCampos", new { contrasenaInput = SharedState.ContrasenaInput });
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> IngresarCaracterUsuario(string caracter, string clientId)
        {
            SharedState.UsuarioInput += caracter;
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarCampos", new { usuarioInput = SharedState.UsuarioInput });
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> IngresarCaracterContrasena(string caracter, string clientId)
        {
            SharedState.ContrasenaInput += caracter;
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarCampos", new { contrasenaInput = SharedState.ContrasenaInput });
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> IniciarSesion(string clientId)
        {
            // Envía una señal al cliente para que intente iniciar sesión
            await _hubContext.Clients.Group(clientId).SendAsync("IniciarSesion");

            return Json(new { success = true });
        }
        [HttpGet]
        public IActionResult CheckLoginStatus(string clientId)
        {
            if (SharedState.LoginStatus.ContainsKey(clientId))
            {
                var loginSuccess = SharedState.LoginStatus[clientId];
                // Remover el estado después de verificar
                SharedState.LoginStatus.Remove(clientId);
                return Json(new { success = loginSuccess });
            }
            else
            {
                return Json(new { success = false, pending = true });
            }
        }
        [HttpGet]
        public async Task<IActionResult> CerrarSesion(string clientId)
        {
            // Limpiar el estado compartido asociado con el clientId
            if (SharedState.LoginStatus.ContainsKey(clientId))
            {
                SharedState.LoginStatus.Remove(clientId);
            }
            SharedState.UsuarioInput = "";
            SharedState.ContrasenaInput = "";
            SharedState.CampoSeleccionado = "";

            // Notificar al cliente web para que cierre sesión
            await _hubContext.Clients.Group(clientId).SendAsync("Logout");

            return Json(new { success = true });
        }




    }
}

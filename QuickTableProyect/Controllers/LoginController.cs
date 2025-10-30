using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;                                // EF 6 → Include()
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace QuickTableProyect.Interface
{
    public class LoginController : Controller
    {
        private readonly EmpleadoService _empleadoService;
        private readonly RegistroSesionService _sesionService;
        private readonly SistemaQuickTableContext _ctx = new();      // EF6 DbContext
        private readonly RegistroSesionService sesionService;
        private readonly PasswordService passwordService;



        public LoginController(
            EmpleadoService empleadoService,
            RegistroSesionService sesionService)
        {
            _empleadoService = empleadoService;
            _sesionService = sesionService;
            this.sesionService = sesionService;
            passwordService = new PasswordService();

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
        // ----------- POST Login/Autenticar --------------
        [HttpPost]
        public JsonResult Autenticar(string nombre, string contrasena)
        {
            LimpiarCodigosVencidos();

            var emp = _empleadoService.ObtenerEmpleadoPorNombre(nombre);

            if (emp == null || !passwordService.VerifyPassword(contrasena, emp.Contrasena))
            {
                return Json(new { success = false, message = "Nombre o contraseña incorrectos." });
            }

            // ----- VALIDACIÓN CRÍTICA PARA ADMINISTRADORES -----
            if (emp.Rol == "Admin")
            {
                // Verificar si el admin tiene una tarjeta NFC asignada
                var tieneTarjeta = _ctx.TarjetasRC.Any(t =>
                    t.EmpleadoId == emp.Id &&
                    t.Activa == true);

                if (!tieneTarjeta)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No tienes una tarjeta NFC asignada. Contacta a TI para obtener una tarjeta antes de iniciar sesión."
                    });
                }
            }

            // 1) marca sesión anterior como error
            sesionService.MarcarErroresPendientes(emp.Id);

            // 2) registra nueva conexión
            int regId = sesionService.RegistrarConexion(emp.Id);

            // 3) guarda datos en la sesión de ASP.NET Core
            HttpContext.Session.SetString("Rol", emp.Rol);
            HttpContext.Session.SetString("Id", emp.Id.ToString());
            HttpContext.Session.SetString("Nombre", emp.Nombre);
            HttpContext.Session.SetInt32("RegistroSesionId", regId);
            // FORZAR LA CREACION DE LA COOKIE DE SESION INMEDIATAMENTE
            var sessionId = HttpContext.Session.Id;

            // ----- segundo factor SOLO para administradores -----
            if (emp.Rol == "Admin")
            {
                // Limpiar códigos 2FA previos del empleado antes de crear uno nuevo
                var codigosAnteriores = _ctx.Codigos2FA
                    .Where(c => c.EmpleadoId == emp.Id && !c.Confirmado)
                    .ToList();
                _ctx.Codigos2FA.RemoveRange(codigosAnteriores);

                string code = new Random().Next(100000, 999999).ToString();
                Guid navId = Guid.NewGuid();

                _ctx.Codigos2FA.Add(new Codigo2FA
                {
                    Codigo = code,
                    NavegadorId = navId,
                    EmpleadoId = emp.Id,
                    Expiracion = DateTime.Now.AddMinutes(10),
                    Confirmado = false,
                    Usado = false // Agregar campo para mejor control
                });

                _ctx.SaveChanges();

                return Json(new
                {
                    success = true,
                    requiere2FA = true,
                    code,
                    nav = navId // usado por el modal en JS
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


        // ----------- POST /Login/Confirmar2FA (CORREGIDO) --------------
        // ----------- POST /Login/Confirmar2FA (CON DEBUG) --------------
        [HttpPost]
        public IActionResult Confirmar2FA(string navId, string uidFisico, string textoEscrito)
        {
            try
            {
                if (!Guid.TryParse(navId, out Guid navegadorId))
                {                    
                    return BadRequest("ID de navegador inválido");
                }

                System.Console.WriteLine($"[DEBUG] Iniciando 2FA - NavId: {navegadorId}, UidFisico: {uidFisico}, TextoEscrito: {textoEscrito}");

                // PRIMERO: Buscar el código 2FA pendiente
                var codigo2FA = _ctx.Codigos2FA
                    .Include(c => c.Empleado)
                    .FirstOrDefault(c => c.NavegadorId == navegadorId &&
                                        !c.Confirmado &&
                                        !c.Usado &&
                                        c.Expiracion > DateTime.Now);

                if (codigo2FA == null)
                {
                   
                    return BadRequest("No hay petición 2FA válida pendiente para este navegador");
                }

                

                // SEGUNDO: Buscar TODAS las tarjetas que coincidan con los datos físicos
                var tarjetasCandidatas = _ctx.TarjetasRC
                    .Include(t => t.Empleado)
                    .Where(t => t.Activa &&
                               t.Empleado.Rol == "Admin" &&
                               t.UidFisico == uidFisico &&         // UID del chip
                               t.Uid == textoEscrito.Trim())        // UID que escribimos
                    .ToList();

                

                // TERCERO: Buscar la tarjeta que pertenezca al empleado correcto
                var tarjetaCorrecta = tarjetasCandidatas
                    .FirstOrDefault(t => t.EmpleadoId == codigo2FA.EmpleadoId);

                if (tarjetaCorrecta == null)
                {                   

                    foreach (var t in tarjetasCandidatas)
                    {
                        System.Console.WriteLine($"[DEBUG] - EmpleadoId: {t.EmpleadoId} ({t.Empleado?.Nombre})");
                    }

                    // Marcar el código como usado para evitar reintentos
                    codigo2FA.Usado = true;
                    _ctx.SaveChanges();
                    return BadRequest("Tarjeta no autorizada para este usuario");
                }

               

                // CUARTO: Confirmar 2FA exitoso
                codigo2FA.Confirmado = true;
                codigo2FA.Usado = true;
                _ctx.SaveChanges();

                System.Console.WriteLine($"[DEBUG] 2FA confirmado exitosamente para {tarjetaCorrecta.Empleado?.Nombre}");
                return Ok("2FA confirmado exitosamente");
            }
            catch (Exception ex)
            {
                
                return BadRequest($"Error en 2FA: {ex.Message}");
            }
        }


        // ----------- GET /Login/Check2FA --------------
        [HttpGet]
        public JsonResult Check2FA(Guid navId)
        {
            bool ok = _ctx.Codigos2FA.Any(c => c.NavegadorId == navId && c.Confirmado);
            return Json(ok);
        }

        // ----------- POST /Login/CancelarTodo2FA (NUEVO) --------------
        [HttpPost]
        public JsonResult CancelarTodo2FA()
        {
            try
            {
                string empIdString = HttpContext.Session.GetString("Id");
                if (int.TryParse(empIdString, out int empId))
                {
                    // Cancelar todos los códigos 2FA pendientes del empleado
                    var codigosPendientes = _ctx.Codigos2FA
                        .Where(c => c.EmpleadoId == empId && !c.Confirmado)
                        .ToList();

                    foreach (var codigo in codigosPendientes)
                    {
                        codigo.Usado = true;  // Marcarlos como usados para cancelarlos
                    }
                    _ctx.SaveChanges();
                }

                // Limpiar códigos vencidos globalmente
                LimpiarCodigosVencidos();

                return Json(new { success = true, message = "Códigos 2FA cancelados" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ----------- utilitario: ¿tiene 2FA pendiente? --------------
        private bool Requiere2FA(int empId) =>
            _ctx.Codigos2FA.Any(c => c.EmpleadoId == empId &&
                                     !c.Confirmado &&
                                     !c.Usado &&
                                     c.Expiracion > DateTime.Now);

        // ----------- GET /Login/Logout --------------
        public IActionResult Logout()
        {
            int? regId = HttpContext.Session.GetInt32("RegistroSesionId");
            if (regId.HasValue) _sesionService.RegistrarDesconexion(regId.Value);

            // Limpiar códigos 2FA pendientes del usuario al hacer logout
            string empIdString = HttpContext.Session.GetString("Id");
            if (int.TryParse(empIdString, out int empId))
            {
                var codigosPendientes = _ctx.Codigos2FA
                    .Where(c => c.EmpleadoId == empId && !c.Confirmado)
                    .ToList();

                foreach (var codigo in codigosPendientes)
                {
                    codigo.Usado = true;
                }
                _ctx.SaveChanges();
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        // ----------- Limpieza mejorada de códigos vencidos --------------
        private void LimpiarCodigosVencidos()
        {
            // Eliminar códigos vencidos por tiempo
            var vencidosPorTiempo = _ctx.Codigos2FA
                .Where(c => c.Expiracion < DateTime.Now)
                .ToList();

            // Eliminar códigos marcados como usados (cancelados o completados)
            var usados = _ctx.Codigos2FA
                .Where(c => c.Usado)
                .ToList();

            _ctx.Codigos2FA.RemoveRange(vencidosPorTiempo);
            _ctx.Codigos2FA.RemoveRange(usados);
            _ctx.SaveChanges();
        }

        // ----------- JSON FinalizarDia --------------
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

        // ----------- QR Generator --------------
        [HttpGet]
        public IActionResult GenerarQR()
        {
            try
            {
                // Obtener la IP y puerto del servidor
                var request = HttpContext.Request;
                var host = request.Host.Host;
                var port = request.Host.Port ?? 5000;

                // Construir la URL completa
                string urlLogin = $"http://{host}:{port}/Login";

                // Generar el codigo QR usando PngByteQRCode (compatible con .NET 8)
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(urlLogin, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);

                // Obtener el QR como array de bytes
                byte[] qrCodeImage = qrCode.GetGraphic(20);

                // Convertir a base64
                string base64String = Convert.ToBase64String(qrCodeImage);

                return Json(new
                {
                    success = true,
                    qrImage = $"data:image/png;base64,{base64String}",
                    url = urlLogin
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

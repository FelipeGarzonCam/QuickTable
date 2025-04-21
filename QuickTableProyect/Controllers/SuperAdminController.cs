using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;

namespace QuickTableProyect.Controllers
{
    public class SuperAdminController : Controller
    {
        private readonly ISuperAdminService _service;

        public SuperAdminController(ISuperAdminService service)
        {
            _service = service;
        }

        // 1. Lista de superadmins
        [HttpGet]
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "SuperAdmin") return RedirectToAction("Index", "Login");
            // Nota: Tu interfaz no tiene un método para obtener superadmins, 
            // así que esto probablemente necesita ajustarse
            // var list = await _service.GetAllSuperAdminsAsync();
            return View();
        }

        // 2. Crear SuperAdmin (usuario + contraseña + tarjeta vacía)
        [HttpGet]
        public IActionResult CreateAdmin() => View();

        [HttpPost]
        public async Task<IActionResult> CreateAdmin(string nombre, string contrasena)
        {
            if (!ModelState.IsValid) return View();
            await _service.CreateSuperAdminAsync(nombre, contrasena);
            return RedirectToAction(nameof(Index));
        }

        // 3. Asignar tarjeta RFID
        [HttpGet]
        public IActionResult AssignTag(int id) // Cambiado de Guid a int
        {
            ViewBag.SuperAdminId = id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignTag(int superAdminId, string tagUid) // Cambiado de Guid a int
        {
            if (!ModelState.IsValid) return View();
            await _service.CreateTagAsync(superAdminId, tagUid);
            return RedirectToAction(nameof(Index));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Dominio;
using QuickTableProyect.Aplicacion;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;

namespace QuickTableProyect.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly MenuService _menuService;
        private readonly EmpleadoService _empleadoService;
        private readonly RegistroSesionService _registroSesionService;

        public AdministradorController(MenuService menuService, EmpleadoService empleadoService, RegistroSesionService registroSesionService)
        {
            _menuService = menuService;
            _empleadoService = empleadoService;
            _registroSesionService = registroSesionService;
            // Establecer el contexto de licencia de EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        [HttpGet]
        public IActionResult ObtenerMenuItemsTest()
        {
            var data = _menuService.ObtenerMenuItems();
            return Json(data);
        }

        [HttpGet]
        public IActionResult ObtenerMenuItems(int page = 1, int pageSize = 10, string sortColumn = "Id", string sortOrder = "asc", string searchTerm = "")
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            var query = _menuService.ObtenerMenuItems().AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m => m.Nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }
            switch (sortColumn)
            {
                case "Nombre":
                    query = sortOrder == "asc" ? query.OrderBy(m => m.Nombre) : query.OrderByDescending(m => m.Nombre);
                    break;
                case "Categoria":
                    query = sortOrder == "asc" ? query.OrderBy(m => m.Categoria) : query.OrderByDescending(m => m.Categoria);
                    break;
                case "Precio":
                    query = sortOrder == "asc" ? query.OrderBy(m => m.Precio) : query.OrderByDescending(m => m.Precio);
                    break;
                default:
                    query = sortOrder == "asc" ? query.OrderBy(m => m.Id) : query.OrderByDescending(m => m.Id);
                    break;
            }
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.Nombre,
                    m.Categoria,
                    m.Descripcion,
                    Precio = m.Precio.ToString("N0")
                })
                .ToList();
            return Json(new { items, totalPages });
        }

        public IActionResult ModificarMenu()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            var menuItems = _menuService.ObtenerMenuItems();
            return View(menuItems);
        }

        public IActionResult CrearMenuItem()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Categorias = new List<string> { "Entradas", "Platos fuertes", "Postres", "Bebidas" };
            return View();
        }

        [HttpPost]
        public IActionResult CrearMenuItem(MenuItem menuItem)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            if (ModelState.IsValid)
            {
                menuItem.Precio = decimal.Truncate(menuItem.Precio);
                _menuService.CrearMenuItem(menuItem);
                return RedirectToAction(nameof(ModificarMenu));
            }
            ViewBag.Categorias = new List<string> { "Entradas", "Platos fuertes", "Postres", "Bebidas" };
            return View(menuItem);
        }

        public IActionResult EditarMenuItem(int id)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            var menuItem = _menuService.ObtenerMenuItemPorId(id);
            ViewBag.Categorias = new List<string> { "Entradas", "Platos fuertes", "Postres", "Bebidas" };
            return View(menuItem);
        }

        [HttpPost]
        public IActionResult EditarMenuItem(MenuItem menuItem)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            if (ModelState.IsValid)
            {
                menuItem.Precio = decimal.Truncate(menuItem.Precio);
                _menuService.ActualizarMenuItem(menuItem);
                return RedirectToAction(nameof(ModificarMenu));
            }
            ViewBag.Categorias = new List<string> { "Entradas", "Platos fuertes", "Postres", "Bebidas" };
            return View(menuItem);
        }

        public IActionResult EliminarMenuItem(int id)
        {
            _menuService.EliminarMenuItem(id);
            return RedirectToAction(nameof(ModificarMenu));
        }

        public IActionResult ModificarEmpleados()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            var empleados = _empleadoService.ObtenerEmpleados();
            return View(empleados);
        }

        public IActionResult CrearEmpleado()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Roles = new List<string> { "Mesero", "Cocina", "Admin", "Cajero" };
            return View();
        }

        [HttpPost]
        public IActionResult CrearEmpleado(Empleado empleado)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            if (ModelState.IsValid)
            {
                _empleadoService.CrearEmpleado(empleado);
                return RedirectToAction(nameof(ModificarEmpleados));
            }
            ViewBag.Roles = new List<string> { "Mesero", "Cocina", "Admin", "Cajero" };
            return View(empleado);
        }

        public IActionResult EditarEmpleado(int id)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            var empleado = _empleadoService.ObtenerEmpleadoPorId(id);
            ViewBag.Roles = new List<string> { "Mesero", "Cocina", "Admin", "Cajero" };
            return View(empleado);
        }

        [HttpPost]
        public IActionResult EditarEmpleado(Empleado empleado)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            if (ModelState.IsValid)
            {
                _empleadoService.ActualizarEmpleado(empleado);
                return RedirectToAction(nameof(ModificarEmpleados));
            }
            ViewBag.Roles = new List<string> { "Mesero", "Cocina", "Admin", "Cajero" };
            return View(empleado);
        }

        public IActionResult EliminarEmpleado(int id)
        {
            _empleadoService.EliminarEmpleado(id);
            return RedirectToAction(nameof(ModificarEmpleados));
        }

        [HttpGet]
        public IActionResult RegistroSesiones()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            var registros = _registroSesionService.ObtenerRegistrosPorFechaRolIdNombre(null, "", null, "");
            return View(registros);
        }
        [HttpGet]
        public IActionResult ObtenerRegistrosSesiones(DateTime? fecha, string rol, int? empleadoId, string nombre, int pageNumber = 1, int pageSize = 10)
        {
            // Verificar si el usuario es Admin
            var rolSession = HttpContext.Session.GetString("Rol");
            if (rolSession != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }

            // Obtener registros filtrados
            var registros = _registroSesionService.ObtenerRegistrosPorFechaRolIdNombre(fecha, rol, empleadoId, nombre);

            // Calcular paginación
            var totalItems = registros.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Aplicar paginación y seleccionar campos necesarios
            var paginatedRegistros = registros
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    empleadoId = r.EmpleadoId,
                    nombre = r.Empleado.Nombre,
                    rol = r.Empleado.Rol,
                    fechaHoraConexion = r.FechaHoraConexion.ToString("yyyy-MM-ddTHH:mm:ss"), // Formato ISO 8601
                    fechaHoraDesconexion = r.FechaHoraDesconexion?.ToString("yyyy-MM-ddTHH:mm:ss") ?? null // Null si está en línea
                })
                .ToList();

            // Devolver respuesta JSON
            return Json(new
            {
                registros = paginatedRegistros,
                currentPage = pageNumber,
                totalPages = totalPages
            });
        }

        [HttpGet]
        public IActionResult DescargarRegistrosSesionesExcel(DateTime? fecha, string rol, int? empleadoId, string nombre)
        {
            var rolSession = HttpContext.Session.GetString("Rol");
            if (rolSession != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }

            var registros = _registroSesionService.ObtenerRegistrosPorFechaRolIdNombre(fecha, rol, empleadoId, nombre);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Registros de Sesiones");

                worksheet.Cells[1, 1].Value = "ID Empleado";
                worksheet.Cells[1, 2].Value = "Nombre Empleado";
                worksheet.Cells[1, 3].Value = "Rol";
                worksheet.Cells[1, 4].Value = "Fecha y Hora Conexión";
                worksheet.Cells[1, 5].Value = "Fecha y Hora Desconexión";

                int row = 2;
                foreach (var registro in registros)
                {
                    worksheet.Cells[row, 1].Value = registro.EmpleadoId;
                    worksheet.Cells[row, 2].Value = registro.Empleado.Nombre;
                    worksheet.Cells[row, 3].Value = registro.Empleado.Rol;
                    worksheet.Cells[row, 4].Value = registro.FechaHoraConexion.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells[row, 5].Value = registro.FechaHoraDesconexion?.ToString("dd/MM/yyyy HH:mm:ss") ?? "En línea";
                    row++;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "RegistrosSesiones.xlsx");
            }
        }
    }
}
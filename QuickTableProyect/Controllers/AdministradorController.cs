using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Dominio;
using QuickTableProyect.Aplicacion;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using QuickTableProyect.Aplicacion;
using static QuickTableProyect.Aplicacion.PedidoService;

namespace QuickTableProyect.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly MenuService _menuService;
        private readonly EmpleadoService _empleadoService;
        private readonly RegistroSesionService _registroSesionService;
        private readonly IPedidoService _pedidoService;

        public AdministradorController(MenuService menuService, EmpleadoService empleadoService, RegistroSesionService registroSesionService, IPedidoService pedidoService)
        {
            _menuService = menuService;
            _empleadoService = empleadoService;
            _registroSesionService = registroSesionService;
            // Establecer el contexto de licencia de EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            _pedidoService = pedidoService;
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
        public IActionResult ObtenerRegistrosSesiones(DateTime? fecha, string rol, int? empleadoId, string nombre, int pageNumber = 1, int pageSize = 10, string sortColumn = "FechaHoraConexion", string sortOrder = "desc")
        {
            // Verificar si el usuario es Admin
            var rolSession = HttpContext.Session.GetString("Rol");
            if (rolSession != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }

            // Obtener registros filtrados
            var registros = _registroSesionService.ObtenerRegistrosPorFechaRolIdNombre(fecha, rol, empleadoId, nombre);

            // Aplicar ordenamiento
            IOrderedEnumerable<RegistroSesion> sortedRegistros;
            switch (sortColumn)
            {
                case "EmpleadoId":
                    sortedRegistros = sortOrder == "asc" ?
                        registros.OrderBy(r => r.EmpleadoId) :
                        registros.OrderByDescending(r => r.EmpleadoId);
                    break;
                case "Nombre":
                    sortedRegistros = sortOrder == "asc" ?
                        registros.OrderBy(r => r.Empleado.Nombre) :
                        registros.OrderByDescending(r => r.Empleado.Nombre);
                    break;
                case "Rol":
                    sortedRegistros = sortOrder == "asc" ?
                        registros.OrderBy(r => r.Empleado.Rol) :
                        registros.OrderByDescending(r => r.Empleado.Rol);
                    break;
                case "FechaHoraDesconexion":
                    sortedRegistros = sortOrder == "asc"
                      ? registros.OrderBy(r =>
                          r.FechaHoraDesconexion == "Error al cerrar sesión"
                            ? DateTime.MinValue
                          : r.FechaHoraDesconexion is null
                            ? DateTime.MaxValue
                          : DateTime.Parse(r.FechaHoraDesconexion))
                      : registros.OrderByDescending(r =>
                          r.FechaHoraDesconexion is null
                            ? DateTime.MinValue
                          : r.FechaHoraDesconexion == "Error al cerrar sesión"
                            ? DateTime.MaxValue
                          : DateTime.Parse(r.FechaHoraDesconexion));
                    break;
                case "FechaHoraConexion":
                default:
                    sortedRegistros = sortOrder == "asc" ?
                        registros.OrderBy(r => r.FechaHoraConexion) :
                        registros.OrderByDescending(r => r.FechaHoraConexion);
                    break;
            }

            // Calcular paginación
            var totalItems = sortedRegistros.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Aplicar paginación y seleccionar campos necesarios
            var paginatedRegistros = sortedRegistros
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    empleadoId = r.EmpleadoId,
                    nombre = r.Empleado.Nombre,
                    rol = r.Empleado.Rol,
                    fechaHoraConexion = r.FechaHoraConexion.ToString("yyyy-MM-ddTHH:mm:ss"),
                    fechaHoraDesconexion = r.FechaHoraDesconexion
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
        public IActionResult DescargarRegistrosSesionesExcel(DateTime? fecha, string rol, int? empleadoId, string nombre, string sortColumn = "FechaHoraConexion", string sortOrder = "desc")
        {
            var rolSession = HttpContext.Session.GetString("Rol");
            if (rolSession != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }

            // Obtener registros con los filtros aplicados
            var registros = _registroSesionService.ObtenerRegistrosPorFechaRolIdNombre(fecha, rol, empleadoId, nombre);

            // Aplicar ordenamiento
            IOrderedEnumerable<RegistroSesion> sortedRegistros;
            switch (sortColumn)
            {
                case "EmpleadoId":
                    sortedRegistros = sortOrder == "asc" ?
                        registros.OrderBy(r => r.EmpleadoId) :
                        registros.OrderByDescending(r => r.EmpleadoId);
                    break;
                case "Nombre":
                    sortedRegistros = sortOrder == "asc" ?
                        registros.OrderBy(r => r.Empleado.Nombre) :
                        registros.OrderByDescending(r => r.Empleado.Nombre);
                    break;
                case "Rol":
                    sortedRegistros = sortOrder == "asc" ?
                        registros.OrderBy(r => r.Empleado.Rol) :
                        registros.OrderByDescending(r => r.Empleado.Rol);
                    break;
                case "FechaHoraDesconexion":
                    sortedRegistros = sortOrder == "asc"
                      ? registros.OrderBy(r =>
                          r.FechaHoraDesconexion == "Error al cerrar sesión"
                            ? DateTime.MinValue
                          : r.FechaHoraDesconexion is null
                            ? DateTime.MaxValue
                          : DateTime.Parse(r.FechaHoraDesconexion))
                      : registros.OrderByDescending(r =>
                          r.FechaHoraDesconexion is null
                            ? DateTime.MinValue
                          : r.FechaHoraDesconexion == "Error al cerrar sesión"
                            ? DateTime.MaxValue
                          : DateTime.Parse(r.FechaHoraDesconexion));
                    break;
                case "FechaHoraConexion":
                default:
                    sortedRegistros = sortOrder == "asc" ?
                        registros.OrderBy(r => r.FechaHoraConexion) :
                        registros.OrderByDescending(r => r.FechaHoraConexion);
                    break;
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Registros de Sesiones");

                worksheet.Cells[1, 1].Value = "ID Empleado";
                worksheet.Cells[1, 2].Value = "Nombre Empleado";
                worksheet.Cells[1, 3].Value = "Rol";
                worksheet.Cells[1, 4].Value = "Fecha y Hora Conexión";
                worksheet.Cells[1, 5].Value = "Fecha y Hora Desconexión";

                int row = 2;
                foreach (var registro in sortedRegistros)
                {
                    worksheet.Cells[row, 1].Value = registro.EmpleadoId;
                    worksheet.Cells[row, 2].Value = registro.Empleado.Nombre;
                    worksheet.Cells[row, 3].Value = registro.Empleado.Rol;
                    worksheet.Cells[row, 4].Value = registro.FechaHoraConexion.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells[row, 5].Value = registro.FechaHoraDesconexion is null ? "En línea": registro.FechaHoraDesconexion == "Error al cerrar sesión"
                          ? registro.FechaHoraDesconexion
                        : registro.FechaHoraDesconexion;
                    row++;
                }

                // Estilizar tabla
                using (var range = worksheet.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"RegistrosSesiones_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
        }
        // 1) La vista vacía con los filtros y el table placeholder
        [HttpGet]
        public IActionResult HistorialPedidos()
        {

            return View();
        }

        // 2) Endpoint AJAX que DataTables (o tu script) llamará para datos paginados
        [HttpPost]
        public async Task<JsonResult> GetHistorialPedidos(
            int draw,            // DataTables draw counter
            int start,           // offset
            int length,          // page size
            int? pedidoId,
            string mesa,
            int? meseroId,
            DateTime? fechaDesde,
            DateTime? fechaHasta
        )
        {
            var filter = new PedidoFilter
            {
                PedidoId = pedidoId,
                Mesa = mesa,
                MeseroId = meseroId,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
            };

            // Desencapsulamos la tupla con tipos concretos
            (IEnumerable<PedidoHistorialViewModel> items, int total)
                = await _pedidoService.ObtenerHistorialAsync(filter, start, length);


            return Json(new
            {
                draw = draw,
                recordsTotal = total,
                recordsFiltered = total,
                data = items
            });
        }
    }
}
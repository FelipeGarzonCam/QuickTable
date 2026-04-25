using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Dominio;
using QuickTableProyect.Aplicacion;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using System;
using System.IO;
using System.Threading.Tasks;
using static QuickTableProyect.Aplicacion.PedidoService;
using QuickTableProyect.Persistencia.Datos;



namespace QuickTableProyect.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly MenuService _menuService;
        private readonly EmpleadoService _empleadoService;
        private readonly RegistroSesionService _registroSesionService;
        private readonly IPedidoService _pedidoService;
        private readonly HistorialPedidoService _historialPedidoService;
        private readonly PasswordService passwordService = new PasswordService();

        // AGREGAR contexto para gestión de tarjetas
        private readonly SistemaQuickTableContext ctx;

        // Asegúrate de que este sea el único constructor en la clase
        public AdministradorController(MenuService menuService, EmpleadoService empleadoService,
            RegistroSesionService registroSesionService, IPedidoService pedidoService,
            HistorialPedidoService historialPedidoService)
        {
            _menuService = menuService;
            _empleadoService = empleadoService;
            _registroSesionService = registroSesionService;
            _pedidoService = pedidoService;
            _historialPedidoService = historialPedidoService;

            // AGREGAR inicialización del contexto
            ctx = new SistemaQuickTableContext();

            // Establecer el contexto de licencia de EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public IActionResult Index()
        {
            // Verificar sesión básica
            var idStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(idStr))
                return RedirectToAction("Index", "Login");
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            
            var dosFaCompletado = HttpContext.Session.GetString("2FACompletado");
            if (dosFaCompletado != "true")
            {
                // La cookie existe pero el 2FA no se completó en este login
                // Redirigir al login para que complete el flujo
                HttpContext.Session.Remove("Id");
                HttpContext.Session.Remove("Nombre");
                HttpContext.Session.Remove("Rol");
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
            ViewBag.Roles = new List<string> { "Mesero", "Cocina", "Cajero" };
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

            foreach (var error in ModelState)
            {
                System.Diagnostics.Debug.WriteLine($"Campo: {error.Key}, Errores: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            if (ModelState.IsValid)
            {
                empleado.Activo = true;
                empleado.Contrasena = passwordService.HashPassword(empleado.Contrasena);  // HASHEAR LA CONTRASEÑAce.CrearEmpleado");
                System.Diagnostics.Debug.WriteLine("Llamando empleadoService.CrearEmpleado");
                try
                {
                    _empleadoService.CrearEmpleado(empleado);
                    System.Diagnostics.Debug.WriteLine("Empleado creado exitosamente");
                    return RedirectToAction(nameof(ModificarEmpleados));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR CREANDO EMPLEADO: {ex.Message}");
                    ModelState.AddModelError("", $"Error: {ex.Message}");
                }
            }

            ViewBag.Roles = new List<string> { "Mesero", "Cocina", "Cajero", "Admin" };
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
            ViewBag.Roles = new List<string> { "Mesero", "Cocina", "Cajero" };
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
                // Solo hashear si se proporcionó una nueva contraseña
                if (!string.IsNullOrEmpty(empleado.Contrasena))
                {
                    empleado.Contrasena = passwordService.HashPassword(empleado.Contrasena);
                }
                else
                {
                    // Mantener la contraseña actual del empleado
                    var empleadoActual = _empleadoService.ObtenerEmpleadoPorId(empleado.Id);
                    if (empleadoActual != null)
                    {
                        empleado.Contrasena = empleadoActual.Contrasena;
                    }
                }

                _empleadoService.ActualizarEmpleado(empleado);
                return RedirectToAction(nameof(ModificarEmpleados));
            }
            ViewBag.Roles = new List<string> { "Mesero", "Cocina", "Cajero" };
            return View(empleado);
        }


        public IActionResult EliminarEmpleado(int id)
        {
            _empleadoService.EliminarEmpleado(id);
            return RedirectToAction(nameof(ModificarEmpleados));
        }
        [HttpGet]
        public IActionResult RegistroSesiones(DateTime? fecha = null, int? empleadoId = null, string rol = null)
        {
            var rolActual = HttpContext.Session.GetString("Rol");
            if (rolActual != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }

            var empleados = _empleadoService.ObtenerEmpleados();
            ViewBag.Empleados = empleados;
            ViewBag.Roles = new List<string> { "Mesero", "Cocina", "Cajero", "Admin" };

            var jornadas = _registroSesionService.ObtenerJornadasDiarias(fecha, empleadoId, rol);

            if (jornadas == null)
            {
                jornadas = new List<JornadaDiariaDto>();
            }

            ViewBag.Fecha = fecha;
            ViewBag.EmpleadoId = empleadoId;
            ViewBag.RolSeleccionado = rol;

            return View(jornadas);
        }

        [HttpGet]
        public IActionResult ExportarExcel(DateTime? fecha = null, int? empleadoId = null, string rol = null)
        {
            var jornadas = _registroSesionService.ObtenerJornadasDiarias(fecha, empleadoId, rol);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Jornadas Laborales");

                worksheet.Cells[1, 1].Value = "Empleado";
                worksheet.Cells[1, 2].Value = "Rol";
                worksheet.Cells[1, 3].Value = "Fecha";
                worksheet.Cells[1, 4].Value = "Primera Entrada";
                worksheet.Cells[1, 5].Value = "Última Salida";
                worksheet.Cells[1, 6].Value = "Tiempo Trabajado";
                worksheet.Cells[1, 7].Value = "Sesiones";
                worksheet.Cells[1, 8].Value = "Anotación";

                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                int row = 2;
                foreach (var jornada in jornadas)
                {
                    worksheet.Cells[row, 1].Value = jornada.EmpleadoNombre;
                    worksheet.Cells[row, 2].Value = jornada.Rol;
                    worksheet.Cells[row, 3].Value = jornada.Fecha.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 4].Value = jornada.PrimeraEntrada?.ToString("HH:mm") ?? "-";
                    worksheet.Cells[row, 5].Value = jornada.UltimaSalida?.ToString("HH:mm") ?? "-";
                    worksheet.Cells[row, 6].Value = $"{(int)jornada.TiempoTrabajado.TotalHours}h {jornada.TiempoTrabajado.Minutes}m";
                    worksheet.Cells[row, 7].Value = jornada.TotalSesiones;
                    worksheet.Cells[row, 8].Value = jornada.Anotacion;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"Jornadas_Laborales_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
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
                    fechaHoraDesconexion = r.FechaHoraDesconexion,
                    marcoTarjetaSalida = r.MarcoTarjetaSalida // NUEVO CAMPO
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
                worksheet.Cells[1, 6].Value = "Método Salida"; // NUEVA COLUMNA

                int row = 2;
                foreach (var registro in sortedRegistros)
                {
                    worksheet.Cells[row, 1].Value = registro.EmpleadoId;
                    worksheet.Cells[row, 2].Value = registro.Empleado.Nombre;
                    worksheet.Cells[row, 3].Value = registro.Empleado.Rol;
                    worksheet.Cells[row, 4].Value = registro.FechaHoraConexion.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells[row, 5].Value = registro.FechaHoraDesconexion is null ? "En línea" : registro.FechaHoraDesconexion == "Error al cerrar sesión"
                          ? registro.FechaHoraDesconexion
                        : registro.FechaHoraDesconexion;
                    worksheet.Cells[row, 6].Value = registro.MarcoTarjetaSalida ? "Tarjeta NFC" : "Manual/Timeout"; // NUEVA COLUMNA
                    row++;
                }

                // Estilizar tabla
                using (var range = worksheet.Cells[1, 1, 1, 6]) // ACTUALIZADO A 6 COLUMNAS
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
        //AdministradorController:
        [HttpGet]
        public IActionResult HistorialPedidos()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        // Endpoint AJAX que DataTables (o tu script) llamará para datos paginados
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

        [HttpGet]
        public IActionResult ObtenerHistorialPedidos(DateTime? fecha, string metodoPago, string nombreMesero, int? pedidoId, string mesa, int pageNumber = 1, int pageSize = 10, string sortColumn = "FechaHora", string sortOrder = "desc")
        {
            // Verificar si el usuario es Admin
            var rolSession = HttpContext.Session.GetString("Rol");
            if (rolSession != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }

            // Obtener historial de pedidos con filtros
            var query = _historialPedidoService.ObtenerHistorialPedidos().AsQueryable();

            // Aplicar filtros
            if (fecha.HasValue)
            {
                query = query.Where(h => h.FechaHora.Date == fecha.Value.Date);
            }

            if (!string.IsNullOrEmpty(metodoPago))
            {
                query = query.Where(h => h.MetodoPago == metodoPago);
            }

            if (!string.IsNullOrEmpty(nombreMesero))
            {
                query = query.Where(h => h.MeseroNombre != null &&
                                       h.MeseroNombre.Contains(nombreMesero, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(mesa))
            {
                query = query.Where(h => h.NumeroMesa.ToString().Contains(mesa));
            }

            // Aplicar ordenamiento
            IOrderedQueryable<HistorialPedido> sortedQuery;
            switch (sortColumn)
            {
                case "Id":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.Id) :
                        query.OrderByDescending(h => h.Id);
                    break;
                case "Mesa":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.NumeroMesa) :
                        query.OrderByDescending(h => h.NumeroMesa);
                    break;
                case "MeseroId":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.MeseroId) :
                        query.OrderByDescending(h => h.MeseroId);
                    break;
                case "Total":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.Total) :
                        query.OrderByDescending(h => h.Total);
                    break;
                case "MetodoPago":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.MetodoPago) :
                        query.OrderByDescending(h => h.MetodoPago);
                    break;
                case "FechaHora":
                default:
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.FechaHora) :
                        query.OrderByDescending(h => h.FechaHora);
                    break;
            }

            // Calcular paginación
            var totalItems = sortedQuery.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Obtener pedidos con GetHistorialPedidosViewModel
            var pedidos = sortedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(h => new PedidoHistorialViewModel
                {
                    PedidoId = h.Id,
                    Mesa = h.NumeroMesa.ToString(),
                    MeseroId = h.MeseroId,
                    NombreMesero = h.MeseroNombre,
                    FechaCreacion = h.FechaHora,
                    TiempoCocinaAListo = h.CocinaListoAt.HasValue
                        ? (h.CocinaListoAt.Value > h.FechaHora
                           ? h.CocinaListoAt.Value - h.FechaHora
                           : TimeSpan.Zero)
                        : TimeSpan.Zero,
                    TiempoListoAAceptado = (h.MeseroAceptadoAt.HasValue && h.CocinaListoAt.HasValue)
                        ? (h.MeseroAceptadoAt.Value > h.CocinaListoAt.Value
                           ? h.MeseroAceptadoAt.Value - h.CocinaListoAt.Value
                           : TimeSpan.Zero)
                        : TimeSpan.Zero,
                    TiempoTotal = h.MeseroAceptadoAt.HasValue
                        ? (h.MeseroAceptadoAt.Value > h.FechaHora
                           ? h.MeseroAceptadoAt.Value - h.FechaHora
                           : TimeSpan.Zero)
                        : TimeSpan.Zero,
                    Total = h.Total + h.Propina,
                    MedioPago = h.MetodoPago
                })
                .ToList();

            // Devolver respuesta JSON
            return Json(new
            {
                pedidos = pedidos,
                currentPage = pageNumber,
                totalPages = totalPages
            });
        }

        [HttpGet]
        public IActionResult DescargarHistorialPedidosExcel(DateTime? fecha, string metodoPago, string nombreMesero, int? pedidoId, string mesa, string sortColumn = "FechaHora", string sortOrder = "desc")
        {
            var rolSession = HttpContext.Session.GetString("Rol");
            if (rolSession != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }

            // Obtener historial de pedidos con filtros
            var query = _historialPedidoService.ObtenerHistorialPedidos().AsQueryable();

            // Aplicar filtros
            if (fecha.HasValue)
            {
                query = query.Where(h => h.FechaHora.Date == fecha.Value.Date);
            }

            if (!string.IsNullOrEmpty(metodoPago))
            {
                query = query.Where(h => h.MetodoPago == metodoPago);
            }

            if (!string.IsNullOrEmpty(nombreMesero))
            {
                query = query.Where(h => h.MeseroNombre != null &&
                                       h.MeseroNombre.Contains(nombreMesero, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(mesa))
            {
                query = query.Where(h => h.NumeroMesa.ToString().Contains(mesa));
            }

            // Aplicar ordenamiento
            IOrderedQueryable<HistorialPedido> sortedQuery;
            switch (sortColumn)
            {
                case "Id":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.Id) :
                        query.OrderByDescending(h => h.Id);
                    break;
                case "Mesa":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.NumeroMesa) :
                        query.OrderByDescending(h => h.NumeroMesa);
                    break;
                case "MeseroId":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.MeseroId) :
                        query.OrderByDescending(h => h.MeseroId);
                    break;
                case "Total":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.Total) :
                        query.OrderByDescending(h => h.Total);
                    break;
                case "MetodoPago":
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.MetodoPago) :
                        query.OrderByDescending(h => h.MetodoPago);
                    break;
                case "FechaHora":
                default:
                    sortedQuery = sortOrder == "asc" ?
                        query.OrderBy(h => h.FechaHora) :
                        query.OrderByDescending(h => h.FechaHora);
                    break;
            }

            // Procedemos a generar el Excel
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Historial de Pedidos");

                worksheet.Cells[1, 1].Value = "ID Pedido";
                worksheet.Cells[1, 2].Value = "Mesa";
                worksheet.Cells[1, 3].Value = "Mesero";
                worksheet.Cells[1, 4].Value = "Fecha y Hora";
                worksheet.Cells[1, 5].Value = "Tiempo Cocina (min)";
                worksheet.Cells[1, 6].Value = "Tiempo Entrega (min)";
                worksheet.Cells[1, 7].Value = "Tiempo Total (min)";
                worksheet.Cells[1, 8].Value = "Items del Pedido";
                worksheet.Cells[1, 9].Value = "Subtotal";
                worksheet.Cells[1, 10].Value = "IVA";
                worksheet.Cells[1, 11].Value = "Propina";
                worksheet.Cells[1, 12].Value = "Total";
                worksheet.Cells[1, 13].Value = "Método de Pago";

                using (var range = worksheet.Cells[1, 1, 1, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                int row = 2;
                foreach (var pedido in sortedQuery)
                {
                    var tiempoCocina = pedido.CocinaListoAt.HasValue && pedido.CocinaListoAt.Value > pedido.FechaHora
                        ? (pedido.CocinaListoAt.Value - pedido.FechaHora).TotalMinutes
                        : 0;

                    var tiempoEntrega = pedido.MeseroAceptadoAt.HasValue && pedido.CocinaListoAt.HasValue && pedido.MeseroAceptadoAt.Value > pedido.CocinaListoAt.Value
                        ? (pedido.MeseroAceptadoAt.Value - pedido.CocinaListoAt.Value).TotalMinutes
                        : 0;

                    var tiempoTotal = pedido.MeseroAceptadoAt.HasValue && pedido.MeseroAceptadoAt.Value > pedido.FechaHora
                        ? (pedido.MeseroAceptadoAt.Value - pedido.FechaHora).TotalMinutes
                        : 0;

                    var itemsDetalle = string.Join(", ", pedido.Detalles.Select(d => $"{d.Nombre} x{d.Cantidad} (${d.Valor:N0})"));

                    worksheet.Cells[row, 1].Value = pedido.Id;
                    worksheet.Cells[row, 2].Value = pedido.NumeroMesa;
                    worksheet.Cells[row, 3].Value = pedido.MeseroNombre;
                    worksheet.Cells[row, 4].Value = pedido.FechaHora.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cells[row, 5].Value = Math.Round(tiempoCocina, 2);
                    worksheet.Cells[row, 6].Value = Math.Round(tiempoEntrega, 2);
                    worksheet.Cells[row, 7].Value = Math.Round(tiempoTotal, 2);
                    worksheet.Cells[row, 8].Value = itemsDetalle;
                    worksheet.Cells[row, 9].Value = pedido.Subtotal;
                    worksheet.Cells[row, 10].Value = pedido.IVA;
                    worksheet.Cells[row, 11].Value = pedido.Propina;
                    worksheet.Cells[row, 12].Value = pedido.Total + pedido.Propina;
                    worksheet.Cells[row, 13].Value = pedido.MetodoPago;

                    worksheet.Cells[row, 9].Style.Numberformat.Format = "$#,##0";
                    worksheet.Cells[row, 10].Style.Numberformat.Format = "$#,##0";
                    worksheet.Cells[row, 11].Style.Numberformat.Format = "$#,##0";
                    worksheet.Cells[row, 12].Style.Numberformat.Format = "$#,##0";

                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"HistorialPedidos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        
        [HttpGet]
        public IActionResult ObtenerDetallePedido(int pedidoId)
        {
            var rolSession = HttpContext.Session.GetString("Rol");
            if (rolSession != "Admin")
            {
                return Json(new { error = "No autorizado" });
            }

            try
            {
                var pedido = _historialPedidoService.ObtenerHistorialPedidos()
                    .FirstOrDefault(p => p.Id == pedidoId);

                if (pedido == null)
                {
                    return Json(new { error = "Pedido no encontrado" });
                }

                var items = pedido.Detalles.Select(d => new
                {
                    nombreItem = d.Nombre,
                    cantidad = d.Cantidad,
                    precioUnitario = d.Valor,
                    subtotal = d.Cantidad * d.Valor
                }).ToList();

                var subtotal = pedido.Total;
                var iva = pedido.IVA;
                var propina = pedido.Propina;
                var total = subtotal + propina;

                return Json(new
                {
                    items = items,
                    subtotal = subtotal,
                    iva = iva,
                    propina = propina,
                    total = total
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener detalles: " + ex.Message });
            }
        }



        // GESTIÓN DE TARJETAS NFC

        [HttpGet]
        public JsonResult EstadoAsignacionTarjeta(int empleadoId, string codigo)
        {
            try
            {
                var codigo2fa = ctx.Codigos2FA
                    .FirstOrDefault(c => c.EmpleadoId == empleadoId &&
                                        c.Codigo == codigo &&
                                        c.EsParaTarjetaEmpleado == true);

                if (codigo2fa == null)
                    return Json(new { estado = "error" });

                if (codigo2fa.Confirmado)
                    return Json(new { estado = "asignada" });

                if (codigo2fa.Expiracion <= DateTime.Now)
                    return Json(new { estado = "expirado" });

                return Json(new { estado = "pendiente" });
            }
            catch (Exception ex)
            {
                return Json(new { estado = "error", mensaje = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GestionarTarjetas()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Admin")
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                var empleados = ctx.Empleados
                    .Where(e => e.Rol != "Admin") 
                    .OrderBy(e => e.Nombre)
                    .ToList();

                return View(empleados);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar empleados: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public JsonResult AsignarTarjetaEmpleado([FromBody] AsignarTarjetaRequest request)
        {
            try
            {
                if (request == null || request.empleadoId <= 0)
                {
                    return Json(new { success = false, message = "Datos inválidos" });
                }

                var empleado = ctx.Empleados.Find(request.empleadoId);
                if (empleado == null)
                {
                    return Json(new { success = false, message = "Empleado no encontrado" });
                }

                // Generar código de sesión (6 dígitos)
                string codigoSesion = Random.Shared.Next(100000, 1000000).ToString();

                // Crear registro temporal en tabla Codigos2FA (reutilizamos la tabla existente)
                var codigoTemporal = new Codigo2FA
                {
                    EmpleadoId = request.empleadoId,
                    Codigo = codigoSesion,
                    Expiracion = DateTime.Now.AddMinutes(10), // 10 minutos para usar el código
                    EsParaTarjetaEmpleado = true // Indica que es para asignar tarjeta de empleado
                };

                // Limpiar códigos vencidos primero
                var vencidos = ctx.Codigos2FA.Where(c => c.Expiracion < DateTime.Now).ToList();
                ctx.Codigos2FA.RemoveRange(vencidos);

                ctx.Codigos2FA.Add(codigoTemporal);
                ctx.SaveChanges();

                return Json(new
                {
                    success = true,
                    codigoSesion = codigoSesion,
                    nombreEmpleado = empleado.Nombre,
                    rolEmpleado = empleado.Rol
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult DesasignarTarjetaEmpleado([FromBody] DesasignarTarjetaRequest request)
        {
            try
            {
                if (request == null || request.empleadoId <= 0)
                {
                    return Json(new { success = false, message = "ID de empleado inválido" });
                }

                var empleado = ctx.Empleados.Find(request.empleadoId);
                if (empleado == null)
                {
                    return Json(new { success = false, message = "Empleado no encontrado" });
                }

                if (string.IsNullOrEmpty(empleado.TarjetaUID))
                {
                    return Json(new { success = false, message = $"{empleado.Nombre} no tiene tarjeta asignada" });
                }

                // Desasignar tarjeta
                empleado.TarjetaUID = null;
                ctx.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Tarjeta desasignada de {empleado.Nombre}",
                    nombreEmpleado = empleado.Nombre
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public IActionResult Configuracion()
        {
            if (HttpContext.Session.GetString("Rol") != "Admin")
                return RedirectToAction("Index", "Login");
            if (HttpContext.Session.GetString("2FACompletado") != "true")
                return RedirectToAction("Index", "Login");

            var svc = new QuickTableProyect.Aplicacion.ConfiguracionService();
            ViewBag.NombreRestaurante = svc.GetNombreRestaurante();
            return View();
        }

        [HttpPost]
        public IActionResult GuardarConfiguracion(string nombreRestaurante)
        {
            if (HttpContext.Session.GetString("Rol") != "Admin")
                return Json(new { success = false, message = "No autorizado" });

            if (string.IsNullOrWhiteSpace(nombreRestaurante) || nombreRestaurante.Trim().Length > 100)
                return Json(new { success = false, message = "Nombre inválido (máx. 100 caracteres)" });

            var svc = new QuickTableProyect.Aplicacion.ConfiguracionService();
            svc.ActualizarNombreRestaurante(nombreRestaurante.Trim());
            return Json(new { success = true });
        }

        // Liberar recursos del contexto
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ctx?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ============= CLASES AUXILIARES =============

    public class AsignarTarjetaRequest
    {
        public int empleadoId { get; set; }
    }

    public class DesasignarTarjetaRequest
    {
        public int empleadoId { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc; 
using QuickTableProyect.Dominio; 
using QuickTableProyect.Aplicacion; 
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder; 

namespace QuickTableProyect.Interface
{
    public class AdministradorController : Controller 
    {
        private readonly MenuService _menuService; 
        private readonly EmpleadoService _empleadoService; 

        public AdministradorController(MenuService menuService, EmpleadoService empleadoService)
        {
            _menuService = menuService; 
            _empleadoService = empleadoService; 
        }

        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol"); 

            // Verificar si el rol es Admin
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
                return Unauthorized(); 
            }

            var query = _menuService.ObtenerMenuItems().AsQueryable(); 

            // Aplicar filtro de búsqueda si existe
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m => m.Nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)); // Filtra por nombre
            }

            // Ordenar según los parámetros recibidos
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

            // Total de registros después del filtrado
            var totalItems = query.Count();

            // Calcular total de páginas
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Obtener los registros para la página actual
            var items = query
                .Skip((page - 1) * pageSize) // Omite los registros de páginas anteriores
                .Take(pageSize) // Toma los registros de la página actual
                .Select(m => new {
                    m.Id,
                    m.Nombre,
                    m.Categoria,
                    m.Descripcion,
                    Precio = m.Precio.ToString("N0") // Formatear el precio sin decimales
                })
                .ToList();

            return Json(new { items, totalPages }); 
        }

        public IActionResult ModificarMenu()
        {
            var rol = HttpContext.Session.GetString("Rol"); 

            // Verificar si el rol es Admin
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
                _menuService.ActualizarMenuItem(menuItem); // Actualiza el item del menú
                return RedirectToAction(nameof(ModificarMenu)); // Redirige a la vista de modificar menú
            }
            ViewBag.Categorias = new List<string> { "Entradas", "Platos fuertes", "Postres", "Bebidas" }; 
            return View(menuItem); 
        }

        public IActionResult EliminarMenuItem(int id)
        {
            _menuService.EliminarMenuItem(id); // Elimina el item del menú por ID
            return RedirectToAction(nameof(ModificarMenu)); // Redirige a la vista de modificar menú
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
    }
}

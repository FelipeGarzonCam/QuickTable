using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using QuickTableProyect.Dominio;
using System.Linq;

namespace QuickTableProyect.Interface
{
    public class MeseroController : Controller
    {
        private readonly PedidoService _pedidoService;
        private readonly MenuService _menuService;
     
        public MeseroController(PedidoService pedidoService, MenuService menuService)
        {
            _pedidoService = pedidoService;
            _menuService = menuService;

        }

        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");
            var empleadoIdString = HttpContext.Session.GetString("Id");

            if (rol != "Mesero" || !int.TryParse(empleadoIdString, out int empleadoId))
            {
                return RedirectToAction("Index", "Login");
            }

            var pedidos = _pedidoService.ObtenerPedidosPorMesero(empleadoId);
            ViewData["MeseroId"] = empleadoId;
            ViewData["NombreMesero"] = HttpContext.Session.GetString("Nombre");
            ViewData["Title"] = $"Pedidos Activos - {ViewData["NombreMesero"]}";

            return View(pedidos);
        }

        public IActionResult NuevoPedido()
        {
            var rol = HttpContext.Session.GetString("Rol");
            var empleadoIdString = HttpContext.Session.GetString("Id");

            if (rol != "Mesero" || !int.TryParse(empleadoIdString, out int empleadoId))
            {
                return RedirectToAction("Index", "Login");
            }

            var categorias = _menuService.ObtenerMenuItems()
                .Select(m => m.Categoria)
                .Distinct()
                .ToList();

            var menuItems = _menuService.ObtenerMenuItems();

            ViewBag.Categorias = categorias;
            ViewData["MeseroId"] = empleadoId;
            ViewData["MeseroNombre"] = HttpContext.Session.GetString("Nombre");
            ViewBag.DetallesComentario = new Dictionary<int, string>();


            return View(menuItems);
        }        

        [HttpPost]
        public JsonResult ConfirmarPedido([FromBody] List<ItemDetalle> detalles, int numeroMesa)
        {
            var empleadoIdString = HttpContext.Session.GetString("Id");
            var empleadoNombre = HttpContext.Session.GetString("Nombre");

            if (!int.TryParse(empleadoIdString, out int empleadoId))
            {
                return Json(new { success = false, message = "Error al identificar al mesero." });
            }


            if (numeroMesa == 0)
            {                
                return Json(new { success = false, message = "Ingrese el numero de mesa" });
            }
            var nuevoPedido = new PedidosActivos
            {
                MeseroId = empleadoId,
                EmpleadoNombre = empleadoNombre,
                NumeroMesa = numeroMesa,
                Detalles = detalles
            };

            _pedidoService.CrearPedido(nuevoPedido);

            return Json(new { success = true, message = "Pedido confirmado exitosamente." });
        }

        public IActionResult EditarPedido(int pedidoId)
        {
            var rol = HttpContext.Session.GetString("Rol");
            var empleadoIdString = HttpContext.Session.GetString("Id");

            if (rol != "Mesero" || !int.TryParse(empleadoIdString, out int empleadoId))
            {
                return RedirectToAction("Index", "Login");
            }

            var pedido = _pedidoService.ObtenerPedidoPorId(pedidoId);
            var detallesComentario = pedido.Detalles.ToDictionary(d => d.MenuItemId, d => d.Comentario ?? "");

            ViewBag.DetallesComentario = detallesComentario;

            if (pedido == null || pedido.MeseroId != empleadoId)
            {
                return RedirectToAction("Index", "Mesero");
            }

            var menuItems = _menuService.ObtenerMenuItems();

            var pedidoDetalles = pedido.Detalles.ToDictionary(d => d.MenuItemId, d => d.Cantidad);

            ViewBag.DetallesPedido = pedidoDetalles;
            ViewBag.Categorias = menuItems.Select(i => i.Categoria).Distinct().ToList();
            ViewData["MeseroId"] = empleadoId;
            ViewData["MeseroNombre"] = HttpContext.Session.GetString("Nombre");
            ViewData["PedidoId"] = pedidoId;

            var itemsOrdenados = menuItems
                .OrderByDescending(item => pedidoDetalles.ContainsKey(item.Id))
                .ToList();

            return View(itemsOrdenados);
        }

        [HttpPost]
        public JsonResult ConfirmarCambios(int pedidoId, [FromBody] List<ItemDetalle> detalles)
        {
            var empleadoIdString = HttpContext.Session.GetString("Id");
            if (!int.TryParse(empleadoIdString, out int empleadoId))
            {
                return Json(new { success = false, message = "Error al identificar al mesero." });
            }

            var pedidoExistente = _pedidoService.ObtenerPedidoPorId(pedidoId);
            if (pedidoExistente == null || pedidoExistente.MeseroId != empleadoId)
            {
                return Json(new { success = false, message = "Pedido no encontrado o sin permisos para editar." });
            }

            // Actualizar el pedido existente
            var pedidoActualizado = new PedidosActivos
            {
                Id = pedidoId,
                MeseroId = empleadoId,
                EmpleadoNombre = HttpContext.Session.GetString("Nombre"),
                NumeroMesa = pedidoExistente.NumeroMesa,
                Detalles = detalles
            };

            // Llamar al método ActualizarPedido
            _pedidoService.ActualizarPedido(pedidoActualizado);

            return Json(new { success = true, message = "Pedido actualizado exitosamente." });
        }
        [HttpGet]
        public IActionResult ObtenerPedidosMesero()
        {
            var rol = HttpContext.Session.GetString("Rol");
            var empleadoIdString = HttpContext.Session.GetString("Id");

            if (rol != "Mesero" || !int.TryParse(empleadoIdString, out int empleadoId))
            {
                return Unauthorized();
            }

            var pedidos = _pedidoService.ObtenerPedidosPorMesero(empleadoId)
                 .Select(p => new
                 {
                     p.Id,
                     p.NumeroMesa,
                     p.Estado,
                     p.Subtotal,
                     p.IVA,
                     p.Total,
                     Detalles = p.Detalles
                         .Select(d => new
                         {
                             d.Nombre,
                             d.Cantidad,
                             d.Subtotal,
                             comentario = d.Comentario ?? ""   // ← aquí agregas el comentario
                         })
                         .ToList()
                 })
                 .ToList();

            return Json(pedidos);
        }


    }
}

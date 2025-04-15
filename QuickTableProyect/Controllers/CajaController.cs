using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using System.Linq;
using System;
using System.Data.Entity; 



namespace QuickTableProyect.Interface
{
    public class CajaController : Controller
    {
        private readonly PedidoService _pedidoService;
        private readonly HistorialPedidoService _historialPedidoService;

        public CajaController(PedidoService pedidoService, HistorialPedidoService historialPedidoService)
        {
            _pedidoService = pedidoService;
            _historialPedidoService = historialPedidoService;
        }

        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");

            if (rol != "Cajero")
            {
                return RedirectToAction("Index", "Login");
            }

            ViewData["Title"] = "Pedidos Activos";
            return View();
        }
        public IActionResult Historial()
        {
            var rol = HttpContext.Session.GetString("Rol");

            if (rol != "Cajero")
            {
                return RedirectToAction("Index", "Login");
            }

            ViewData["Title"] = "Historial de Pedidos";
            return View();
        }
        public IActionResult Factura()
        {
            var rol = HttpContext.Session.GetString("Rol");

            if (rol != "Cajero")
            {
                return RedirectToAction("Index", "Login");
            }

            ViewData["Title"] = "Historial de Pedidos";
            return View();
        }
        
        [HttpGet]
        public IActionResult ObtenerHistorialPedidos(int pageNumber = 1, int pageSize = 10, string fecha = null, int? pedidoId = null, int? meseroId = null)
        {
            var totalPedidos = _historialPedidoService.ObtenerTotalHistorialPedidos(fecha, pedidoId, meseroId);
            var pedidos = _historialPedidoService.ObtenerHistorialPedidosPaginados(pageNumber, pageSize, fecha, pedidoId, meseroId)
                .Select(p => new
                {
                    id = p.Id,
                    numeroMesa = p.NumeroMesa,
                    meseroId = p.MeseroId,
                    meseroNombre = p.MeseroNombre,
                    fechaHora = p.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                    subtotal = p.Subtotal,
                    iva = p.IVA,
                    total = p.Total,
                    propina = p.Propina,
                    metodoPago = p.MetodoPago,
                    efectivoRecibido = p.EfectivoRecibido,
                    cambio = p.Cambio,
                    detalles = p.Detalles.Select(d => new
                    {
                        nombre = d.Nombre,
                        cantidad = d.Cantidad,
                        valor = d.Valor,
                        subtotal = d.Subtotal
                    }).ToList()
                })
                .ToList();

            return Json(new { totalPedidos, pedidos });
        }

        [HttpGet]
        public IActionResult ObtenerPedidosActivos()
        {
            var pedidos = _pedidoService.ObtenerPedidosActivos()
                .Select(p => new
                {
                    id = p.Id,
                    numeroMesa = p.NumeroMesa,
                    meseroId = p.MeseroId,
                    meseroNombre = p.EmpleadoNombre,
                    estado = p.Estado,
                    subtotal = p.Subtotal,
                    iva = p.IVA,
                    total = p.Total,
                    detalles = p.Detalles.Select(d => new
                    {
                        nombre = d.Nombre,
                        cantidad = d.Cantidad,
                        valor = d.Valor,
                        subtotal = d.Subtotal
                    }).ToList()
                })
                .ToList();

            return Json(pedidos);
        }
        [HttpPost]
        public IActionResult FinalizarPedido(int pedidoId, decimal propina, string metodoPago, decimal? efectivoRecibido, decimal? cambio)
        {
            var pedido = _pedidoService.ObtenerPedidoPorId(pedidoId);

            if (pedido == null)
            {
                return Json(new { success = false, message = "Pedido no encontrado." });
            }

            // Crear el historial de pedido
            var historialPedido = new HistorialPedido
            {
                NumeroMesa = pedido.NumeroMesa,
                MeseroId = pedido.MeseroId,
                MeseroNombre = pedido.EmpleadoNombre,
                FechaHora = DateTime.Now,
                Subtotal = pedido.Subtotal,
                IVA = pedido.IVA,
                Total = pedido.Total,
                Propina = propina,
                MetodoPago = metodoPago,
                EfectivoRecibido = efectivoRecibido,
                Cambio = cambio,
                Detalles = pedido.Detalles.Select(d => new HistorialDetalle
                {
                    MenuItemId = d.MenuItemId,
                    Nombre = d.Nombre,
                    Valor = d.Valor,
                    Cantidad = d.Cantidad
                }).ToList()
            };

            // Guardar en el historial
            _historialPedidoService.CrearHistorialPedido(historialPedido);

            // Eliminar el pedido activo
            _pedidoService.EliminarPedido(pedidoId);

            // Generar URL de la factura
            string facturaUrl = Url.Action("GenerarFactura", "Caja", new { historialPedidoId = historialPedido.Id }, Request.Scheme);

            return Json(new { success = true, facturaUrl = facturaUrl });
        }
        public IActionResult GenerarFactura(int historialPedidoId)
        {
            var pedido = _historialPedidoService.ObtenerHistorialPedidoPorId(historialPedidoId);

            if (pedido == null)
            {
                return NotFound("Pedido no encontrado.");
            }       
            return View("Factura", pedido);
        }



    }
}


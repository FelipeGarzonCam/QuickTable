using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using System;
using System.Data.Entity;
using System.Linq;

namespace QuickTableProyect.Controllers
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
            var pedidos = _pedidoService.ObtenerPedidosActivosNoListos();
            return View(pedidos);
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
            ViewData["Title"] = "Factura";
            return View();
        }

        [HttpGet]
        public IActionResult ObtenerPedidosCaja()
        {
            var pedidos = _pedidoService.ObtenerPedidosActivosNoListos()
                .Select(p => new
                {
                    id = p.Id,
                    numeroMesa = p.NumeroMesa,
                    detalles = p.Detalles
                        .Where(d => d.Cantidad > d.CantidadPreparada)
                        .Select(d => new
                        {
                            d.Nombre,
                            cantidadPendiente = d.Cantidad - d.CantidadPreparada
                        })
                        .ToList()
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
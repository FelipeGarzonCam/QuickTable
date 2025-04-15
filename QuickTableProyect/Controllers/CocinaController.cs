using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using QuickTableProyect.Interface.Hubs;
using QuickTableProyect.Models;
using System;
using System.Data.Entity;
using System.Linq;

namespace QuickTableProyect.Interface
{
    public class CocinaController : Controller
    {
        private readonly PedidoService _pedidoService;
        private readonly IHubContext<ComunicacionHub> _hubContext;

        public CocinaController(PedidoService pedidoService, IHubContext<ComunicacionHub> hubContext)
        {
            _pedidoService = pedidoService;
            _hubContext = hubContext;
        }
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("Rol");

            if (rol != "Cocina")
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        [HttpGet]
        public IActionResult ObtenerPedidosCocina()
        {
            var pedidos = _pedidoService.ObtenerPedidosPendientesCocina()
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
        public IActionResult MarcarPedidoListo(int pedidoId)
        {
            _pedidoService.MarcarPedidoComoListo(pedidoId);
            return Json(new { success = true });
        }
        //NODE
        [HttpGet]
        public async Task<IActionResult> AbrirModalPedido(string clientId)
        {
            SharedState.ModalAbierto = true;
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarModal", new { modalAbierto = true, pedidoIdModal = SharedState.PedidoIdModal });
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> CerrarModalPedido(string clientId)
        {
            SharedState.ModalAbierto = false;
            SharedState.PedidoIdModal = "";
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarModal", new { modalAbierto = false, pedidoIdModal = "" });
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> BorrarContenidoModal(string clientId)
        {
            SharedState.PedidoIdModal = "";
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarModal", new { pedidoIdModal = "" });
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> IngresarDigitoModal(string digito, string clientId)
        {
            SharedState.PedidoIdModal += digito;
            await _hubContext.Clients.Group(clientId).SendAsync("ActualizarModal", new
            {
                modalAbierto = true,
                pedidoIdModal = SharedState.PedidoIdModal
            });
            return Json(new { success = true });
        }


        [HttpGet]
        public async Task<IActionResult> MarcarPedidoModalComoListo(string clientId)
        {
            if (int.TryParse(SharedState.PedidoIdModal, out int pedidoId))
            {
                _pedidoService.MarcarPedidoComoListo(pedidoId);
                // Limpiar el estado después de marcar como listo
                SharedState.PedidoIdModal = "";
                SharedState.ModalAbierto = false;
                await _hubContext.Clients.Group(clientId).SendAsync("ActualizarModal", new { modalAbierto = false, pedidoIdModal = "" });
                return Json(new { success = true });
            }
            await _hubContext.Clients.Group(clientId).SendAsync("MostrarError", new { message = "ID de pedido inválido." });
            return Json(new { success = false, message = "ID de pedido inválido." });
        }

    }
}

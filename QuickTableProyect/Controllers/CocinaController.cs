using Microsoft.AspNetCore.Mvc;
using QuickTableProyect.Aplicacion;
using QuickTableProyect.Dominio;
using System;
using System.Data.Entity;
using System.Linq;

namespace QuickTableProyect.Interface
{
    public class CocinaController : Controller
    {
        private readonly PedidoService _pedidoService;

        public CocinaController(PedidoService pedidoService)
        {
            _pedidoService = pedidoService;
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
        

    }
}

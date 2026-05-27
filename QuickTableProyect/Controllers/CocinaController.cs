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
                .OrderByDescending(p => p.FechaCreacion)
                .Select(p => new
                {
                    id = p.Id,
                    numeroMesa = p.NumeroMesa,
                    fechaCreacion = p.FechaCreacion,
                    detalles = p.Detalles
                        .Where(d => d.Cantidad > d.CantidadPreparada)
                        .Select(d => new
                        {
                            nombre = d.Nombre,
                            comentario = d.Comentario,
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

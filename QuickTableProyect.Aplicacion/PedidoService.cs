using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System.Collections.Generic;
using System.Data.Entity; 
using System.Linq;
using QuickTableProyect.Aplicacion;        

namespace QuickTableProyect.Aplicacion
{
    public class PedidoService : IPedidoService
    {
        private readonly SistemaQuickTableContext _context;

        public PedidoService(SistemaQuickTableContext context)
        {
            _context = context;
        }

        // Obtener pedidos por mesero
        public List<PedidosActivos> ObtenerPedidosPorMesero(int meseroId)
        {
            return _context.PedidosActivos
                .Where(p => p.MeseroId == meseroId)
                .Include(p => p.Detalles)
                .ToList();
        }

        // Crear un nuevo pedido
        public void CrearPedido(PedidosActivos pedido)
        {
            pedido.FechaCreacion = DateTime.Now;
            pedido.Estado = "En Preparación"; // Estado inicial

            foreach (var detalle in pedido.Detalles)
            {
                var menuItem = _context.MenuItems.Find(detalle.MenuItemId);
                if (menuItem != null)
                {
                    detalle.Nombre = menuItem.Nombre;
                    detalle.Valor = menuItem.Precio;
                }
                // Asociar el detalle al pedido
                detalle.PedidoActivoId = pedido.Id;                
                detalle.CantidadPreparada = 0; // Inicializar en 0
            }

            CalcularTotales(pedido);
            _context.PedidosActivos.Add(pedido);
            _context.SaveChanges();
        }

        // Obtener un pedido por su ID
        public PedidosActivos ObtenerPedidoPorId(int pedidoId)
        {
            return _context.PedidosActivos
                .Include(p => p.Detalles)
                .FirstOrDefault(p => p.Id == pedidoId);
        }

        // Calcular totales del pedido
        private void CalcularTotales(PedidosActivos pedido)
        {
            pedido.Subtotal = pedido.Detalles.Sum(d => d.Cantidad * d.Valor);
            pedido.IVA = pedido.Subtotal * 0.19m;
            pedido.Total = pedido.Subtotal + pedido.IVA;
        }

        // Eliminar un pedido
        public void EliminarPedido(int pedidoId)
        {
            var pedidoExistente = _context.PedidosActivos
                .Include(p => p.Detalles)
                .FirstOrDefault(p => p.Id == pedidoId);

            if (pedidoExistente != null)
            {
                _context.ItemDetalles.RemoveRange(pedidoExistente.Detalles);
                _context.PedidosActivos.Remove(pedidoExistente);
                _context.SaveChanges();
            }
        }

        public void ActualizarPedido(PedidosActivos pedido)
        {
            var pedidoExistente = _context.PedidosActivos
                .Include(p => p.Detalles)
                .FirstOrDefault(p => p.Id == pedido.Id);

            if (pedidoExistente != null)
            {
                // Actualizar las propiedades del pedido existente
                pedidoExistente.MeseroId = pedido.MeseroId;
                pedidoExistente.EmpleadoNombre = pedido.EmpleadoNombre;
                pedidoExistente.NumeroMesa = pedido.NumeroMesa;

                if (pedidoExistente.Estado == "Listo")
                {
                    pedidoExistente.Estado = "Editado";
                }

                // Actualizar los detalles del pedido
                var detallesExistentes = pedidoExistente.Detalles.ToList();

                // 1. Eliminar detalles que ya no están en el pedido
                foreach (var detalleExistente in detallesExistentes)
                {
                    var detalleNuevo = pedido.Detalles.FirstOrDefault(d => d.MenuItemId == detalleExistente.MenuItemId);
                    if (detalleNuevo == null)
                    {
                        _context.ItemDetalles.Remove(detalleExistente);
                    }
                }

                // 2. Agregar o actualizar detalles
                foreach (var detalleNuevo in pedido.Detalles)
                {
                    var detalleExistente = pedidoExistente.Detalles.FirstOrDefault(d => d.MenuItemId == detalleNuevo.MenuItemId);
                    if (detalleExistente != null)
                    {
                        int cantidadAnterior = detalleExistente.Cantidad;
                        detalleExistente.Cantidad = detalleNuevo.Cantidad;

                        // Si la cantidad aumentó, CantidadPreparada permanece igual
                        // Si la cantidad disminuyó y CantidadPreparada es mayor, ajustarla
                        if (detalleExistente.Cantidad < detalleExistente.CantidadPreparada)
                        {
                            detalleExistente.CantidadPreparada = detalleExistente.Cantidad;
                        }
                    }
                    else
                    {
                        // El detalle es nuevo, agregarlo
                        var menuItem = _context.MenuItems.Find(detalleNuevo.MenuItemId);
                        if (menuItem != null)
                        {
                            detalleNuevo.Nombre = menuItem.Nombre;
                            detalleNuevo.Valor = menuItem.Precio;
                        }
                        detalleNuevo.PedidoActivoId = pedidoExistente.Id;
                        detalleNuevo.CantidadPreparada = 0; // Inicializar en 0 para nuevos detalles
                        pedidoExistente.Detalles.Add(detalleNuevo);
                    }
                }

                // Recalcular totales
                CalcularTotales(pedidoExistente);

                // Guardar cambios en la base de datos
                _context.SaveChanges();
            }
        }


        // Obtener pedidos activos que no están marcados como "Listo"
        public List<PedidosActivos> ObtenerPedidosActivosNoListos()
        {
            return _context.PedidosActivos
                .Include(p => p.Detalles)
                .Where(p => p.Estado != "Listo")
                .ToList();
        }

        // Obtener un pedido por número de mesa que no esté marcado como "Listo"
        public PedidosActivos ObtenerPedidoPorMesa(int numeroMesa)
        {
            return _context.PedidosActivos
                .Include(p => p.Detalles)
                .FirstOrDefault(p => p.NumeroMesa == numeroMesa && p.Estado != "Listo");
        }

        // Obtener todos los pedidos activos
        public List<PedidosActivos> ObtenerPedidosActivos()
        {
            return _context.PedidosActivos
                .Include(p => p.Detalles)
                .ToList();
        }

        public void MarcarPedidoComoListo(int pedidoId)
        {
            var pedido = _context.PedidosActivos
                .Include(p => p.Detalles)
                .FirstOrDefault(p => p.Id == pedidoId);

            if (pedido != null)
            {
                if (pedido.Estado == EstadosPedido.EnPreparacion || pedido.Estado == EstadosPedido.Editado)
                {
                    foreach (var detalle in pedido.Detalles)
                    {
                        int cantidadPendiente = detalle.Cantidad - detalle.CantidadPreparada;
                        if (cantidadPendiente > 0)
                        {
                            detalle.CantidadPreparada += cantidadPendiente;
                        }
                    }

                    pedido.Estado = EstadosPedido.Listo;
                    pedido.CocinaListoAt = DateTime.Now;
                    _context.SaveChanges();
                }
            }
        }
        public void MarcarPedidoComoAceptado(int pedidoId)
        {
            var pedido = _context.PedidosActivos.Find(pedidoId);
            if (pedido != null && pedido.MeseroAceptadoAt == null)
            {
                pedido.MeseroAceptadoAt = DateTime.Now;
                _context.SaveChanges();
            }
        }
        public List<PedidosActivos> ObtenerPedidosPendientesCocina()
        {
            return _context.PedidosActivos
                .Include(p => p.Detalles)
                .Where(p => (p.Estado == EstadosPedido.EnPreparacion || p.Estado == EstadosPedido.Editado)
                            && p.Detalles.Any(d => d.Cantidad > d.CantidadPreparada))
                .ToList();
        }
        public class PedidoFilter
        {
            public int? PedidoId { get; set; }
            public string Mesa { get; set; }
            public int? MeseroId { get; set; }
            public DateTime? FechaDesde { get; set; }
            public DateTime? FechaHasta { get; set; }
        }

        public async Task<(IEnumerable<PedidoHistorialViewModel> Items, int TotalCount)>
            ObtenerHistorialAsync(PedidoFilter filter, int skip, int take)
        {
            var query = _context.PedidosActivos
                .Include(p => p.EmpleadoNombre)
                .AsQueryable();

            // filtros
            if (filter.PedidoId.HasValue)
                query = query.Where(p => p.Id == filter.PedidoId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Mesa))
                query = query.Where(p => p.NumeroMesa.ToString().Contains(filter.Mesa));
            if (filter.MeseroId.HasValue)
                query = query.Where(p => p.MeseroId == filter.MeseroId.Value);
            if (filter.FechaDesde.HasValue)
                query = query.Where(p => p.FechaCreacion >= filter.FechaDesde.Value);
            if (filter.FechaHasta.HasValue)
                query = query.Where(p => p.FechaCreacion <= filter.FechaHasta.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.FechaCreacion)
                .Skip(skip).Take(take)
                .Select(p => new PedidoHistorialViewModel
                {
                    PedidoId = p.Id,
                    Mesa = p.NumeroMesa.ToString(),

                    MeseroId = p.MeseroId,
                    NombreMesero = p.EmpleadoNombre,
                    FechaCreacion = p.FechaCreacion,
                    TiempoCocinaAListo = p.CocinaListoAt.HasValue
                                          ? p.CocinaListoAt.Value - p.FechaCreacion
                                          : TimeSpan.Zero,
                    TiempoListoAAceptado = (p.MeseroAceptadoAt.HasValue && p.CocinaListoAt.HasValue)
                                          ? p.MeseroAceptadoAt.Value - p.CocinaListoAt.Value
                                          : TimeSpan.Zero,
                    Total = p.Total,
                    MedioPago = p.MedioPago
                })
                .ToListAsync();


            return (items, total);
        }
        public void AceptarPedido(int pedidoId)
        {
            var pedido = _context.PedidosActivos.Find(pedidoId);
            if (pedido != null)
            {
                pedido.MeseroAceptadoAt = DateTime.Now;
                _context.SaveChanges();
            }
        }

    }
}


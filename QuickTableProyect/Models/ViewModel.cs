// En una nueva carpeta, por ejemplo, QuickTableProyect.Dominio.ViewModels
using System.Collections.Generic;

namespace QuickTableProyect.Dominio.ViewModels
{
    public class EditarPedidoViewModel
    {
        public PedidosActivos Pedido { get; set; }
        public List<MenuItem> MenuItems { get; set; }
    }
}

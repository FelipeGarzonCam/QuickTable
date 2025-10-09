using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuickTableProyect.Dominio;
using static QuickTableProyect.Aplicacion.PedidoService;

namespace QuickTableProyect.Aplicacion
{
    public interface IPedidoService
    {
        // Método para el historial con paginación
        Task<(IEnumerable<PedidoHistorialViewModel> Items, int TotalCount)>
            ObtenerHistorialAsync(PedidoFilter filter, int skip, int take);

   
    }
}

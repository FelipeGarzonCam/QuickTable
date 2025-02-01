using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickTableProyect.Dominio;
using QuickTableProyect.Aplicacion;
using System.Collections.Generic;
using System.Linq;

namespace QuickTableProyect.Views.Administrador
{
    public class RegistroSesionesModel : PageModel
    {
        private readonly RegistroSesionService _registroSesionService;

        public RegistroSesionesModel(RegistroSesionService registroSesionService)
        {
            _registroSesionService = registroSesionService;
        }

        public IEnumerable<RegistroSesion> Registros { get; set; }

        public void OnGet()
        {
            Registros = _registroSesionService.ObtenerRegistrosPorFechaRolIdNombre(null, "", null, "");
        }
    }
}
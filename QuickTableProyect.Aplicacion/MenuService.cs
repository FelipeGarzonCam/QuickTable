using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace QuickTableProyect.Aplicacion
{
    public class MenuService
    {
        private readonly SistemaQuickTableContext _context;

        public MenuService(SistemaQuickTableContext context)
        {
            _context = context;
        }

        public List<string> ObtenerCategorias()//machtazo esto no sirvio
        {
            // Obtiene las categorías únicas de los MenuItems
            var categoriasQuery = _context.MenuItems.Select(m => m.Categoria);
            var categoriasUnicas = categoriasQuery.Distinct();
            return categoriasUnicas.ToList();
        }

        public List<MenuItem> ObtenerMenuItems() => _context.MenuItems.ToList();

        public MenuItem ObtenerMenuItemPorId(int id) => _context.MenuItems.Find(id);

        public void CrearMenuItem(MenuItem menuItem)
        {
            _context.MenuItems.Add(menuItem);
            _context.SaveChanges();
        }

        public void ActualizarMenuItem(MenuItem menuItem)
        {
            _context.Entry(menuItem).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void EliminarMenuItem(int id)
        {
            var menuItem = _context.MenuItems.Find(id);
            if (menuItem != null)
            {
                _context.MenuItems.Remove(menuItem);
                _context.SaveChanges();
            }
        }
    }
}

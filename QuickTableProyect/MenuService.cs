using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System.Collections.Generic;
using System.Linq;

namespace QuickTableProyect.Aplicacion
{
    public class MenuService
    {
        private readonly SistemaQuickTableContext _context;

        public MenuService(SistemaQuickTableContext context)
        {
            _context = context; // Corregir la asignación
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
            var existingItem = _context.MenuItems.Find(menuItem.Id);
            if (existingItem != null)
            {
                existingItem.Nombre = menuItem.Nombre;
                existingItem.Precio = menuItem.Precio;
                existingItem.Descripcion = menuItem.Descripcion;
                existingItem.Categoria = menuItem.Categoria;
                _context.SaveChanges();
            }
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
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
            _context = context;
        }

        // Obtener todos los elementos del menú
        public List<MenuItem> ObtenerMenuItems() => _context.MenuItems.ToList();

        // Obtener un elemento específico por su Id
        public MenuItem ObtenerMenuItemPorId(int id) => _context.MenuItems.Find(id);

        // Agregar un nuevo elemento al menú
        public void CrearMenuItem(MenuItem menuItem)
        {
            _context.MenuItems.Add(menuItem);
            _context.SaveChanges();
        }

        // Modificar un elemento existente en el menú
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

        // Eliminar un elemento del menú por su Id
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

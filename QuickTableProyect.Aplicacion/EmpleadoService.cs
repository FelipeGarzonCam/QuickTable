using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace QuickTableProyect.Aplicacion
{
    public class EmpleadoService
    {
        private readonly SistemaQuickTableContext _context;

        public EmpleadoService(SistemaQuickTableContext context)
        {
            _context = context;
        }

        public List<Empleado> ObtenerEmpleados() => _context.Empleados.ToList();

        public Empleado ObtenerEmpleadoPorId(int id) => _context.Empleados.Find(id);

        public Empleado ObtenerEmpleadoPorNombre(string nombre)
        {
            return _context.Empleados.FirstOrDefault(e => e.Nombre == nombre);
        }

        public void CrearEmpleado(Empleado empleado)
        {
            _context.Empleados.Add(empleado);
            _context.SaveChanges();
        }

        public void ActualizarEmpleado(Empleado empleado)
        {
            _context.Entry(empleado).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public void EliminarEmpleado(int id)
        {
            var empleado = _context.Empleados.Find(id);
            if (empleado != null)
            {
                _context.Empleados.Remove(empleado);
                _context.SaveChanges();
            }
        }
    }
}

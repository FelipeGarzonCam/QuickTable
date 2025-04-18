using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace QuickTableProyect.Aplicacion
{
    public class RegistroSesionService
    {
        private readonly SistemaQuickTableContext _context;

        public RegistroSesionService(SistemaQuickTableContext context)
        {
            _context = context;
        }

        public void RegistrarConexion(int empleadoId)
        {
            var registro = new RegistroSesion
            {
                EmpleadoId = empleadoId,
                FechaHoraConexion = DateTime.Now
            };
            _context.RegistroSesiones.Add(registro);
            _context.SaveChanges();
        }

        public void RegistrarDesconexion(int registroId)
        {
            var registro = _context.RegistroSesiones.Find(registroId);
            if (registro != null)
            {
                registro.FechaHoraDesconexion = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public List<RegistroSesion> ObtenerRegistrosPorFecha(DateTime fecha)
        {
            return _context.RegistroSesiones
                .Include(r => r.Empleado)
                .Where(r => DbFunctions.TruncateTime(r.FechaHoraConexion) == DbFunctions.TruncateTime(fecha))
                .ToList();
        }

        public List<RegistroSesion> ObtenerRegistrosPorFechaRolIdNombre(DateTime? fecha, string rol, int? empleadoId, string nombre)
        {
            var query = _context.RegistroSesiones.Include(r => r.Empleado).AsQueryable();

            if (fecha.HasValue)
            {
                query = query.Where(r => DbFunctions.TruncateTime(r.FechaHoraConexion) == DbFunctions.TruncateTime(fecha.Value));
            }

            if (!string.IsNullOrEmpty(rol))
            {
                query = query.Where(r => r.Empleado.Rol.ToLower() == rol.ToLower());
            }

            if (empleadoId.HasValue)
            {
                query = query.Where(r => r.EmpleadoId == empleadoId.Value);
            }

            if (!string.IsNullOrEmpty(nombre))
            {
                query = query.Where(r => r.Empleado.Nombre.ToLower().Contains(nombre.ToLower()));
            }

            return query.ToList();
        }
    }
}
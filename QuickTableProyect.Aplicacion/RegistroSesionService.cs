using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;

namespace QuickTableProyect.Aplicacion
{
    public class RegistroSesionService
    {
        private readonly SistemaQuickTableContext _context;

        public RegistroSesionService(SistemaQuickTableContext context)
        {
            _context = context;
        }
        // Marca como ERROR todas las sesiones previas "En línea"
        public void MarcarErroresPendientes(int empleadoId)
        {
            var abiertas = _context.RegistroSesiones
                 .Where(r => r.EmpleadoId == empleadoId
                          && r.FechaHoraDesconexion == null)
                 .ToList();

            foreach (var reg in abiertas)
            {
                reg.FechaHoraDesconexion = "Error al cerrar sesión";
            }
            _context.SaveChanges();
        }

        public int RegistrarConexion(int empleadoId)
        {
            var registro = new RegistroSesion
            {
                EmpleadoId = empleadoId,
                FechaHoraConexion = DateTime.Now,
                FechaHoraDesconexion = null
            };
            _context.RegistroSesiones.Add(registro);
            _context.SaveChanges();
            return registro.Id;
        }

        public void RegistrarDesconexion(int registroId)
        {
            var registro = _context.RegistroSesiones.Find(registroId);
            if (registro != null && registro.FechaHoraDesconexion == null)
            {
                registro.FechaHoraDesconexion = DateTime.Now
                    .ToString("yyyy-MM-dd HH:mm:ss");
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
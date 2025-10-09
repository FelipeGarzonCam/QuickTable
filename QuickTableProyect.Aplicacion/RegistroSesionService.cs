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
        private readonly SistemaQuickTableContext context;

        public RegistroSesionService(SistemaQuickTableContext context)
        {
            this.context = context;
        }

        public void MarcarErroresPendientes(int empleadoId)
        {
            var abiertas = context.RegistroSesiones
                .Where(r => r.EmpleadoId == empleadoId && r.FechaHoraDesconexion == null)
                .ToList();

            foreach (var reg in abiertas)
            {
                reg.FechaHoraDesconexion = "Error al cerrar sesión";
            }
            context.SaveChanges();
        }

        public int RegistrarConexion(int empleadoId)
        {
            var registro = new RegistroSesion
            {
                EmpleadoId = empleadoId,
                FechaHoraConexion = DateTime.Now,
                FechaHoraDesconexion = null,
                MarcoTarjetaSalida = false
            };
            context.RegistroSesiones.Add(registro);
            context.SaveChanges();
            return registro.Id;
        }

        public void RegistrarDesconexion(int registroId)
        {
            var registro = context.RegistroSesiones.Find(registroId);
            if (registro != null)
            {
                registro.FechaHoraDesconexion = (registro.FechaHoraDesconexion == null)
                    ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    : registro.FechaHoraDesconexion;
            }
            context.SaveChanges();
        }

        public void FinalizarDiaLaboral(int empleadoId)
        {
            var registroActivo = context.RegistroSesiones
                .Where(r => r.EmpleadoId == empleadoId && r.FechaHoraDesconexion == null)
                .OrderByDescending(r => r.FechaHoraConexion)
                .FirstOrDefault();

            if (registroActivo != null)
            {
                registroActivo.FechaHoraDesconexion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                registroActivo.MarcoTarjetaSalida = false;
                context.SaveChanges();
            }
        }

        public List<RegistroSesion> ObtenerRegistrosPorFecha(DateTime fecha)
        {
            return context.RegistroSesiones
                .Include(r => r.Empleado)
                .Where(r => DbFunctions.TruncateTime(r.FechaHoraConexion) == DbFunctions.TruncateTime(fecha))
                .ToList();
        }

        public List<RegistroSesion> ObtenerRegistrosPorFechaRolIdNombre(DateTime? fecha, string rol, int? empleadoId, string nombre)
        {
            var query = context.RegistroSesiones.Include(r => r.Empleado).AsQueryable();

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
                query = query.Where(r => r.Empleado.Nombre.Contains(nombre));
            }

            return query.ToList();
        }

        public List<JornadaDiariaDto> ObtenerJornadasDiarias(DateTime? fecha = null, int? empleadoId = null, string rol = null)
        {
            var query = context.RegistroSesiones
                .Include(r => r.Empleado)
                .AsQueryable();

            if (fecha.HasValue)
            {
                var fechaDate = fecha.Value.Date;
                query = query.Where(r => DbFunctions.TruncateTime(r.FechaHoraConexion) == fechaDate);
            }

            if (empleadoId.HasValue)
            {
                query = query.Where(r => r.EmpleadoId == empleadoId.Value);
            }

            if (!string.IsNullOrEmpty(rol))
            {
                query = query.Where(r => r.Empleado.Rol == rol);
            }

            var registros = query.ToList();

            var jornadas = registros
                .GroupBy(r => new
                {
                    r.EmpleadoId,
                    r.Empleado.Nombre,
                    r.Empleado.Rol,
                    Fecha = r.FechaHoraConexion.Date
                })
                .Select(g =>
                {
                    var primeraEntrada = g.OrderBy(r => r.FechaHoraConexion).First();
                    var ultimaSesion = g.OrderByDescending(r => r.FechaHoraConexion).First();

                    DateTime? ultimaSalida = null;
                    TimeSpan tiempoTrabajado = TimeSpan.Zero;
                    string anotacion = "";
                    bool marcoTarjetaSalida = false;

                    if (ultimaSesion.FechaHoraDesconexion != null && ultimaSesion.FechaHoraDesconexion != "Error al cerrar sesión")
                    {
                        if (DateTime.TryParse(ultimaSesion.FechaHoraDesconexion, out DateTime salidaParsed))
                        {
                            ultimaSalida = salidaParsed;
                            tiempoTrabajado = ultimaSalida.Value - primeraEntrada.FechaHoraConexion;
                            marcoTarjetaSalida = ultimaSesion.MarcoTarjetaSalida;

                            if (marcoTarjetaSalida)
                            {
                                anotacion = "Jornada completada con tarjeta";
                            }
                            else
                            {
                                anotacion = "Jornada completada manualmente";
                            }
                        }
                    }
                    else if (ultimaSesion.FechaHoraDesconexion == "Error al cerrar sesión")
                    {
                        anotacion = "Salida automática por timeout (>18 horas)";
                        ultimaSalida = primeraEntrada.FechaHoraConexion.AddHours(18);
                        tiempoTrabajado = TimeSpan.FromHours(18);
                    }
                    else
                    {
                        anotacion = "Sin marcar salida - Sesión abierta";
                    }

                    return new JornadaDiariaDto
                    {
                        EmpleadoId = g.Key.EmpleadoId,
                        EmpleadoNombre = g.Key.Nombre,
                        Rol = g.Key.Rol,
                        Fecha = g.Key.Fecha,
                        PrimeraEntrada = primeraEntrada.FechaHoraConexion,
                        UltimaSalida = ultimaSalida,
                        TiempoTrabajado = tiempoTrabajado,
                        Anotacion = anotacion,
                        TotalSesiones = g.Count(),
                        MarcoTarjetaSalida = marcoTarjetaSalida
                    };
                })
                .OrderByDescending(j => j.Fecha)
                .ThenBy(j => j.EmpleadoNombre)
                .ToList();

            return jornadas;
        }

    }
}

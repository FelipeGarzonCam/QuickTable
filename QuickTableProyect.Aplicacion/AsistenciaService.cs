using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Data.Entity;
using System.Linq;

namespace QuickTableProyect.Aplicacion
{
    public class AsistenciaService
    {
        private readonly SistemaQuickTableContext context;

        public AsistenciaService(SistemaQuickTableContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Marca la salida de un empleado usando su tarjeta NFC encriptada
        /// </summary>
        /// <param name="tarjetaUIDEncriptada">UID de la tarjeta encriptado</param>
        /// <returns>Resultado de la operación</returns>
        public ResultadoMarcarSalida MarcarSalidaConTarjeta(string tarjetaUIDEncriptada)
        {
            try
            {
                // Buscar empleado por UID de tarjeta encriptada
                var empleado = context.Empleados
                    .FirstOrDefault(e => e.TarjetaUID == tarjetaUIDEncriptada);

                if (empleado == null)
                {
                    return new ResultadoMarcarSalida
                    {
                        Exito = false,
                        Mensaje = "Tarjeta no reconocida. Contacte al administrador."
                    };
                }

                // Buscar el último registro de sesión sin finalizar del día actual
                var hoy = DateTime.Today;
                var registroActivo = context.RegistroSesiones
                    .Where(r => r.EmpleadoId == empleado.Id)
                    .Where(r => DbFunctions.TruncateTime(r.FechaHoraConexion) == hoy)
                    .Where(r => r.FechaHoraDesconexion == null)
                    .OrderByDescending(r => r.FechaHoraConexion)
                    .FirstOrDefault();

                if (registroActivo == null)
                {
                    return new ResultadoMarcarSalida
                    {
                        Exito = false,
                        Mensaje = $"No se encontró registro de ingreso para {empleado.Nombre} el día de hoy."
                    };
                }

                // Marcar salida
                var horaSalida = DateTime.Now;
                registroActivo.FechaHoraDesconexion = horaSalida.ToString("yyyy-MM-dd HH:mm:ss");
                registroActivo.MarcoTarjetaSalida = true;

                context.SaveChanges();

                // Calcular tiempo trabajado
                var tiempoTrabajado = horaSalida - registroActivo.FechaHoraConexion;

                return new ResultadoMarcarSalida
                {
                    Exito = true,
                    Mensaje = "Salida registrada correctamente",
                    NombreEmpleado = empleado.Nombre,
                    RolEmpleado = empleado.Rol,
                    HoraIngreso = registroActivo.FechaHoraConexion.ToString("HH:mm"),
                    HoraSalida = horaSalida.ToString("HH:mm"),
                    TiempoTrabajado = $"{(int)tiempoTrabajado.TotalHours}h {tiempoTrabajado.Minutes}m"
                };
            }
            catch (Exception ex)
            {
                return new ResultadoMarcarSalida
                {
                    Exito = false,
                    Mensaje = $"Error al marcar salida: {ex.Message}"
                };
            }
        }
    }

    // Clase para el resultado de marcar salida
    public class ResultadoMarcarSalida
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string NombreEmpleado { get; set; }
        public string RolEmpleado { get; set; }
        public string HoraIngreso { get; set; }
        public string HoraSalida { get; set; }
        public string TiempoTrabajado { get; set; }
    }
}

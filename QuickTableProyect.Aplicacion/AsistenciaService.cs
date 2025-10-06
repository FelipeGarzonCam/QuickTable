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


        // CAMBIAR el método MarcarSalidaConTarjeta en AsistenciaService.cs:

        public ResultadoMarcarSalida MarcarSalidaConTarjeta(string tarjetaUIDEncriptada)
        {
            try
            {
                                // DECODIFICAR BASE64
                string uidDecimal = "";
                try
                {
                    byte[] data = Convert.FromBase64String(tarjetaUIDEncriptada);
                    uidDecimal = System.Text.Encoding.UTF8.GetString(data);
                    
                }
                catch (Exception ex)
                {
                    
                    return new ResultadoMarcarSalida
                    {
                        Exito = false,
                        Mensaje = "Error decodificando UID de tarjeta"
                    };
                }

                // CONVERTIR DECIMAL A HEXADECIMAL FORMATO CORRECTO
                string uidHex = "";
                try
                {
                    long decimalValue = long.Parse(uidDecimal);
                    uidHex = decimalValue.ToString("X16"); // Convertir a hexadecimal 16 dígitos
                    
                }
                catch (Exception ex)
                {                    
                    uidHex = uidDecimal;
                }

                // BUSCAR empleado con AMBOS FORMATOS
                var empleado = context.Empleados
                    .FirstOrDefault(e => (e.TarjetaUID == uidHex || e.TarjetaUID == uidDecimal) && e.Activo == true);

               

                if (empleado == null)
                {
                    
                    return new ResultadoMarcarSalida
                    {
                        Exito = false,
                        Mensaje = "Tarjeta no reconocida. Contacte al administrador."
                    };
                }

                // EL RESTO IGUAL...
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

                var horaSalida = DateTime.Now;
                registroActivo.FechaHoraDesconexion = horaSalida.ToString("yyyy-MM-dd HH:mm:ss");
                registroActivo.MarcoTarjetaSalida = true;

                context.SaveChanges();
                

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
//puto git
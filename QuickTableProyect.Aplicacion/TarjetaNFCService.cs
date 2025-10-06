using QuickTableProyect.Dominio;
using QuickTableProyect.Persistencia.Datos;
using System;
using System.Linq;

namespace QuickTableProyect.Aplicacion
{
    public class TarjetaNFCService
    {
        private readonly SistemaQuickTableContext context;
        private readonly CryptoService cryptoService; // AGREGAR

        public TarjetaNFCService(SistemaQuickTableContext context)
        {
            this.context = context;
            this.cryptoService = new CryptoService(); // AGREGAR
        }

        /// <summary>
        /// Asigna una tarjeta NFC a un empleado
        /// </summary>
        /// <param name="empleadoId">ID del empleado</param>
        /// <param name="tarjetaUIDSinEncriptar">UID de la tarjeta sin encriptar (leído de la Raspberry)</param>
        /// <returns>Resultado de la operación</returns>
        public ResultadoAsignarTarjeta AsignarTarjetaAEmpleado(int empleadoId, string tarjetaUIDSinEncriptar)
        {
            try
            {
                // Buscar el empleado
                var empleado = context.Empleados.Find(empleadoId);
                if (empleado == null)
                {
                    return new ResultadoAsignarTarjeta
                    {
                        Exito = false,
                        Mensaje = "Empleado no encontrado"
                    };
                }

                // CORREGIDO: Encriptar el UID de la tarjeta usando instancia
                string tarjetaUIDEncriptada = cryptoService.EncriptarUID(tarjetaUIDSinEncriptar);

                // Verificar que la tarjeta no esté ya asignada a otro empleado
                var empleadoConTarjeta = context.Empleados
                    .FirstOrDefault(e => e.TarjetaUID == tarjetaUIDEncriptada && e.Id != empleadoId);

                if (empleadoConTarjeta != null)
                {
                    return new ResultadoAsignarTarjeta
                    {
                        Exito = false,
                        Mensaje = $"Esta tarjeta ya está asignada a {empleadoConTarjeta.Nombre}"
                    };
                }

                // Asignar la tarjeta encriptada al empleado
                empleado.TarjetaUID = tarjetaUIDEncriptada;
                context.SaveChanges();

                return new ResultadoAsignarTarjeta
                {
                    Exito = true,
                    Mensaje = "Tarjeta asignada correctamente",
                    NombreEmpleado = empleado.Nombre,
                    RolEmpleado = empleado.Rol
                };
            }
            catch (Exception ex)
            {
                return new ResultadoAsignarTarjeta
                {
                    Exito = false,
                    Mensaje = $"Error al asignar tarjeta: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Desasigna la tarjeta de un empleado (por pérdida o cambio)
        /// </summary>
        /// <param name="empleadoId">ID del empleado</param>
        /// <returns>Resultado de la operación</returns>
        public ResultadoDesasignarTarjeta DesasignarTarjeta(int empleadoId)
        {
            try
            {
                var empleado = context.Empleados.Find(empleadoId);
                if (empleado == null)
                {
                    return new ResultadoDesasignarTarjeta
                    {
                        Exito = false,
                        Mensaje = "Empleado no encontrado"
                    };
                }

                if (string.IsNullOrEmpty(empleado.TarjetaUID))
                {
                    return new ResultadoDesasignarTarjeta
                    {
                        Exito = false,
                        Mensaje = $"{empleado.Nombre} no tiene tarjeta asignada"
                    };
                }

                // Remover la tarjeta
                empleado.TarjetaUID = null;
                context.SaveChanges();

                return new ResultadoDesasignarTarjeta
                {
                    Exito = true,
                    Mensaje = "Tarjeta desasignada correctamente",
                    NombreEmpleado = empleado.Nombre
                };
            }
            catch (Exception ex)
            {
                return new ResultadoDesasignarTarjeta
                {
                    Exito = false,
                    Mensaje = $"Error al desasignar tarjeta: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Obtiene información de un empleado por su tarjeta NFC
        /// </summary>
        /// <param name="tarjetaUIDEncriptada">UID encriptado de la tarjeta</param>
        /// <returns>Empleado encontrado o null</returns>
        public Empleado ObtenerEmpleadoPorTarjeta(string tarjetaUIDEncriptada)
        {
            return context.Empleados
                .FirstOrDefault(e => e.TarjetaUID == tarjetaUIDEncriptada);
        }
    }

    // Clases para resultados
    public class ResultadoAsignarTarjeta
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string NombreEmpleado { get; set; }
        public string RolEmpleado { get; set; }
    }

    public class ResultadoDesasignarTarjeta
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string NombreEmpleado { get; set; }
    }
}

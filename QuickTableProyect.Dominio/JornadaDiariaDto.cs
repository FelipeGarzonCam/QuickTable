using System;

namespace QuickTableProyect.Dominio
{
    public class JornadaDiariaDto
    {
        public int EmpleadoId { get; set; }
        public string EmpleadoNombre { get; set; }
        public string Rol { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? PrimeraEntrada { get; set; }
        public DateTime? UltimaSalida { get; set; }
        public TimeSpan TiempoTrabajado { get; set; }
        public string Anotacion { get; set; }
        public int TotalSesiones { get; set; }
        public bool MarcoTarjetaSalida { get; set; }
    }
}

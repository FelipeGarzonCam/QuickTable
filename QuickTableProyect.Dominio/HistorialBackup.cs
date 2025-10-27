using System;

namespace QuickTableProyect.Dominio
{
    public class HistorialBackup
    {
        public int Id { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string NombreArchivo { get; set; }
        public string RutaArchivo { get; set; }
        public decimal TamanioMB { get; set; }
        public int EmpleadoId { get; set; }
        public string UsuarioCreador { get; set; }
        public string TipoBackup { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }

        public string RutaCertificado { get; set; }
        public string RutaClavePrivada { get; set; }
    }
}

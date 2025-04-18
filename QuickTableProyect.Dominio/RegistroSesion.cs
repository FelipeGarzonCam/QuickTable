namespace QuickTableProyect.Dominio
{
    public class RegistroSesion
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public DateTime FechaHoraConexion { get; set; }
        public string? FechaHoraDesconexion { get; set; }
        public Empleado Empleado { get; set; } // Relación con Empleado
    }
}
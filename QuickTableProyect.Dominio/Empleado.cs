using System.ComponentModel.DataAnnotations;

namespace QuickTableProyect.Dominio
{
    public class Empleado
    {
        public int Id { get; set; } 
        public string Nombre { get; set; }
        public string Rol { get; set; }

        [MaxLength(60)]  
        public string Contrasena { get; set; }
        public string? TarjetaUID { get; set; }

        public bool Activo { get; set; } = true; 
    }
}


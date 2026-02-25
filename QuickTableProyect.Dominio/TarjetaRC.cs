using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTableProyect.Dominio
{
    public class TarjetaRC
    {
        public int Id { get; set; }
        public string Uid { get; set; } = null!;
        public bool Activa { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaAsignacion { get; set; }

        public int? EmpleadoId { get; set; }
        public Empleado? Empleado { get; set; }

        public string? CodigoSesion { get; set; }
        public string? UidFisico { get; set; }
    }

}


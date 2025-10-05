using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTableProyect.Dominio
{
    public class Codigo2FA
    {
        public int Id { get; set; }
        public string Codigo { get; set; }          // 6 dígitos
        public Guid NavegadorId { get; set; }          // lo ve la Raspberry
        public DateTime Expiracion { get; set; }
        public bool Confirmado { get; set; }

        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; }
    }
}

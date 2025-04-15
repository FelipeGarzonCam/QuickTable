using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTableProyect.Dominio
{
    public static class SharedState
    {
        public static string CampoSeleccionado { get; set; } = "";
        public static string UsuarioInput { get; set; } = "";
        public static string ContrasenaInput { get; set; } = "";
        public static bool ModalAbierto { get; set; } = false;
        public static string PedidoIdModal { get; set; } = "";

        //diccionario para el estado de inicio de sesión
        public static Dictionary<string, bool> LoginStatus { get; set; } = new Dictionary<string, bool>();
    }

}

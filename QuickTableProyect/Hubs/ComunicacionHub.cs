using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace QuickTableProyect.Interface.Hubs
{
    public class ComunicacionHub : Hub
    {
        public async Task UnirseAlGrupo(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);//Identificador único de la conexión actual del cliente.
        }

        public async Task SalirDelGrupo(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }
    }
}

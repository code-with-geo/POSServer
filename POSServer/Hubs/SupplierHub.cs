using Microsoft.AspNetCore.SignalR;

namespace POSServer.Hubs
{
    public class SupplierHub : Hub
    {
        public async Task NotifyClients(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}

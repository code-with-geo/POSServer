﻿using Microsoft.AspNetCore.SignalR;

namespace POSServer.Hubs
{
    public class CustomerHub : Hub
    {
        public async Task NotifyClients(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}

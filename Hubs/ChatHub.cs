using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Hubs;

public class ChatHub : Hub
{
    // Optionally expose methods if you want clients to send via hub.
    // For this app, the server broadcasts via IHubContext after DB ops.
}

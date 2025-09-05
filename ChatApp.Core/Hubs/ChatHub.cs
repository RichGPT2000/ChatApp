using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Core.Hubs;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinChat(int chatId)
    {
        var group = chatId.ToString();
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        _logger.LogInformation("Connection {ConnectionId} joined chat group {Group}", Context.ConnectionId, group);
    }

    public async Task LeaveChat(int chatId)
    {
        var group = chatId.ToString();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        _logger.LogInformation("Connection {ConnectionId} left chat group {Group}", Context.ConnectionId, group);
    }
}

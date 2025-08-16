using ChatApp.Data;
using ChatApp.Models;
using ChatApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Services;

public class ChatService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHubContext<ChatHub> _hub;

    public ChatService(IDbContextFactory<AppDbContext> dbFactory, IHubContext<ChatHub> hub)
    {
        _dbFactory = dbFactory;
        _hub = hub;
    }

    public async Task<List<Chat>> GetChatsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Chats.AsNoTracking().OrderByDescending(c => c.Id).ToListAsync();
    }

    public async Task<Chat?> GetChatAsync(int chatId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Chats.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chatId);
    }

    public async Task<List<Message>> GetMessagesAsync(int chatId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Messages.AsNoTracking()
            .Where(m => m.ChatId == chatId).OrderBy(m => m.SentAtUtc).ToListAsync();
    }

    public async Task<int> CreateChatAsync(string title)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var chat = new Chat { Title = title, CreatedAtUtc = DateTime.UtcNow };
        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("ChatListChanged");
        return chat.Id;
    }

    public async Task DeleteChatAsync(int chatId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var chat = await db.Chats.FindAsync(chatId);
        if (chat is null) return;
        db.Remove(chat);
        await db.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("ChatListChanged");
    }

    public async Task<int> SendMessageAsync(int chatId, string sender, string text)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var msg = new Message
        {
            ChatId = chatId,
            Sender = string.IsNullOrWhiteSpace(sender) ? "Anon" : sender,
            Text = text,
            SentAtUtc = DateTime.UtcNow
        };
        db.Messages.Add(msg);
        await db.SaveChangesAsync();

        // notify all clients that this chat got a new message
        await _hub.Clients.All.SendAsync("MessageAdded", chatId);
        return msg.Id;
    }
}

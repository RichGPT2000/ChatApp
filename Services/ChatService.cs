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

    public async Task<List<Chat>> GetChatsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Chats.AsNoTracking().OrderByDescending(c => c.Id).ToListAsync(ct);
    }

    public async Task<Chat?> GetChatAsync(int chatId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Chats.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chatId, ct);
    }

    public async Task<List<Message>> GetMessagesAsync(int chatId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Messages.AsNoTracking()
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.SentAtUtc)
            .ToListAsync(ct);
    }

    public async Task<int> CreateChatAsync(string title, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var chat = new Chat { Title = title, CreatedAtUtc = DateTime.UtcNow };
        db.Chats.Add(chat);
        await db.SaveChangesAsync(ct);

        await _hub.Clients.All.SendAsync("ChatListChanged", cancellationToken: ct);
        return chat.Id;
    }

    public async Task DeleteChatAsync(int chatId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var chat = await db.Chats.FindAsync(new object?[] { chatId }, ct);
        if (chat is null) return;
        db.Remove(chat);
        await db.SaveChangesAsync(ct);

        await _hub.Clients.All.SendAsync("ChatListChanged", cancellationToken: ct);
    }

    public async Task<int> SendMessageAsync(int chatId, string sender, string text, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var msg = new Message
        {
            ChatId = chatId,
            Sender = string.IsNullOrWhiteSpace(sender) ? "Anon" : sender,
            Text = text,
            SentAtUtc = DateTime.UtcNow
        };
        db.Messages.Add(msg);
        await db.SaveChangesAsync(ct);

        // notify only clients in this chat's group
        await _hub.Clients.Group(chatId.ToString()).SendAsync("MessageAdded", chatId, cancellationToken: ct);
        return msg.Id;
    }
}

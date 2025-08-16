using System.Collections.Generic;

namespace ChatApp.Models;

public class Chat
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Message> Messages { get; set; } = new();
}

namespace ChatApp.Models;

public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string Sender { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    public Chat? Chat { get; set; }
}

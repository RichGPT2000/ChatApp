namespace ChatApp.Models;

public class MessageDto
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string Sender { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime SentAtUtc { get; set; }
}

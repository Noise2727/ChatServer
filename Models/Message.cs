namespace ChatServer.Models;

public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int SenderId { get; set; }
    public string Text { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsEncrypted { get; set; }

    public Chat? Chat { get; set; }
    public User? Sender { get; set; }
}
namespace ChatServer.Models;

public class Chat
{
    public int Id { get; set; }
    public string Name { get; set; } = ""; // для группового чата
    public bool IsPrivate { get; set; }    // true = ЛС, false = группа
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ChatMember> Members { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public class ChatMember
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int UserId { get; set; }

    public Chat? Chat { get; set; }
    public User? User { get; set; }
}
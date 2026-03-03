namespace ChatServer.Models;

public class Chat
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsChannel { get; set; }
    public bool IsPublic { get; set; }  // для поиска
    public int? AdminId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ChatMember> Members { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public class ChatMember
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int UserId { get; set; }
    public bool IsAdmin { get; set; }

    public Chat? Chat { get; set; }
    public User? User { get; set; }
}
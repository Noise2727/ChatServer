namespace ChatServer.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";      // логин (уникальный)
    public string Nickname { get; set; } = "";       // отображаемое имя (может повторяться)
    public string PasswordHash { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
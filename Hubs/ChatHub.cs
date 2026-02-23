using Microsoft.AspNetCore.SignalR;
using ChatServer.Data;
using ChatServer.Models;

namespace ChatServer.Hubs;

public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    public ChatHub(AppDbContext db) => _db = db;

    // Подключиться к комнате чата
    public async Task JoinChat(int chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    public async Task LeaveChat(int chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    // Отправить сообщение
    public async Task SendMessage(int chatId, int senderId, string text)
    {
        // Лёгкое шифрование (Caesar cipher, сдвиг 13 = ROT13)
        string stored = SimpleEncrypt(text);

        var message = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Text = stored,
            IsEncrypted = true
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        // Рассылаем всем в группе (расшифрованный текст)
        await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.ChatId,
            message.SenderId,
            Text = text, // клиент видит оригинал
            message.SentAt
        });
    }

    // ROT13 — простое шифрование
    private static string SimpleEncrypt(string text)
    {
        return new string(text.Select(c =>
        {
            if (c >= 'a' && c <= 'z') return (char)(((c - 'a' + 13) % 26) + 'a');
            if (c >= 'A' && c <= 'Z') return (char)(((c - 'A' + 13) % 26) + 'A');
            return c;
        }).ToArray());
    }
}
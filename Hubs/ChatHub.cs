using Microsoft.AspNetCore.SignalR;
using ChatServer.Data;
using ChatServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatServer.Hubs;

public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    public ChatHub(AppDbContext db) => _db = db;

    public async Task JoinChat(int chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    public async Task LeaveChat(int chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    public async Task SendMessage(int chatId, int senderId, string text)
    {
        var chat = await _db.Chats.FindAsync(chatId);
        if (chat != null && chat.IsChannel && chat.AdminId != senderId)
            return;

        // Сохраняем зашифрованное в базу
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

        var sender = await _db.Users.FindAsync(senderId);

        // Отправляем ОРИГИНАЛЬНЫЙ текст через SignalR
        await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.ChatId,
            message.SenderId,
            Text = text, // оригинальный, не зашифрованный
            SenderName = sender?.Nickname ?? sender?.Username ?? "",
            message.SentAt
        });
    }

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
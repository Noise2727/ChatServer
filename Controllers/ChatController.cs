using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;
using ChatServer.Models;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _db;
    public ChatController(AppDbContext db) => _db = db;

    // Получить все чаты пользователя
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserChats(int userId)
    {
        var chats = await _db.ChatMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Chat)
            .ThenInclude(c => c!.Members)
            .ThenInclude(m => m.User)
            .Select(m => new
            {
                m.Chat!.Id,
                m.Chat.Name,
                m.Chat.IsPrivate,
                Members = m.Chat.Members.Select(x => new { x.UserId, x.User!.Username })
            })
            .ToListAsync();

        return Ok(chats);
    }

    // Создать ЛС или группу
    [HttpPost("create")]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest req)
    {
        // Если ЛС — проверить, не существует ли уже
        if (req.IsPrivate && req.MemberIds.Count == 2)
        {
            var existing = await _db.Chats
                .Where(c => c.IsPrivate)
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c =>
                    c.Members.Any(m => m.UserId == req.MemberIds[0]) &&
                    c.Members.Any(m => m.UserId == req.MemberIds[1]));

            if (existing != null)
                return Ok(new { existing.Id, existing.Name, existing.IsPrivate });
        }

        var chat = new Chat
        {
            Name = req.Name,
            IsPrivate = req.IsPrivate,
            Members = req.MemberIds.Select(id => new ChatMember { UserId = id }).ToList()
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();
        return Ok(new { chat.Id, chat.Name, chat.IsPrivate });
    }

    // История сообщений
    [HttpGet("{chatId}/messages")]
    public async Task<IActionResult> GetMessages(int chatId)
    {
        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId)
            .Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                m.Id,
                m.Text,
                m.SentAt,
                m.SenderId,
                SenderName = m.Sender!.Username,
                m.IsEncrypted
            })
            .ToListAsync();

        return Ok(messages);
    }
}

public class CreateChatRequest
{
    public string Name { get; set; } = "";
    public bool IsPrivate { get; set; }
    public List<int> MemberIds { get; set; } = new();
}
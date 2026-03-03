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

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserChats(int userId)
    {
        var chats = await _db.ChatMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Chat).ThenInclude(c => c!.Members).ThenInclude(m => m.User)
            .Select(m => new
            {
                m.Chat!.Id,
                m.Chat.Name,
                m.Chat.Description,
                m.Chat.IsPrivate,
                m.Chat.IsChannel,
                m.Chat.IsPublic,
                m.Chat.AdminId,
                Members = m.Chat.Members.Select(x => new { x.UserId, x.User!.Username, x.User.Nickname, x.User.AvatarUrl, x.IsAdmin })
            })
            .ToListAsync();
        return Ok(chats);
    }

    // Поиск публичных чатов и каналов
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var chats = await _db.Chats
            .Where(c => c.IsPublic && c.Name.Contains(q))
            .Include(c => c.Members)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.IsPrivate,
                c.IsChannel,
                c.IsPublic,
                c.AdminId,
                MemberCount = c.Members.Count
            })
            .ToListAsync();
        return Ok(chats);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest req)
    {
        if (req.IsPrivate && req.MemberIds.Count == 2)
        {
            var existing = await _db.Chats
                .Where(c => c.IsPrivate).Include(c => c.Members)
                .FirstOrDefaultAsync(c =>
                    c.Members.Any(m => m.UserId == req.MemberIds[0]) &&
                    c.Members.Any(m => m.UserId == req.MemberIds[1]));
            if (existing != null)
                return Ok(new { existing.Id, existing.Name, existing.IsPrivate, existing.IsChannel, existing.IsPublic });
        }

        var chat = new Chat
        {
            Name = req.Name,
            Description = req.Description,
            IsPrivate = req.IsPrivate,
            IsChannel = req.IsChannel,
            IsPublic = req.IsPublic,
            AdminId = req.AdminId,
            Members = req.MemberIds.Select(id => new ChatMember
            {
                UserId = id,
                IsAdmin = id == req.AdminId
            }).ToList()
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();
        return Ok(new { chat.Id, chat.Name, chat.IsPrivate, chat.IsChannel, chat.IsPublic });
    }

    // Добавить участника
    [HttpPost("{chatId}/invite")]
    public async Task<IActionResult> Invite(int chatId, [FromBody] InviteRequest req)
    {
        var chat = await _db.Chats.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == chatId);
        if (chat == null) return NotFound();

        if (chat.Members.Any(m => m.UserId == req.UserId))
            return BadRequest("Пользователь уже в чате");

        chat.Members.Add(new ChatMember { ChatId = chatId, UserId = req.UserId });
        await _db.SaveChangesAsync();
        return Ok();
    }

    // Обновить настройки чата
    [HttpPut("{chatId}")]
    public async Task<IActionResult> UpdateChat(int chatId, [FromBody] UpdateChatRequest req)
    {
        var chat = await _db.Chats.FindAsync(chatId);
        if (chat == null) return NotFound();

        if (req.Name != null) chat.Name = req.Name;
        if (req.Description != null) chat.Description = req.Description;
        if (req.IsPublic.HasValue) chat.IsPublic = req.IsPublic.Value;

        await _db.SaveChangesAsync();
        return Ok(new { chat.Id, chat.Name, chat.Description, chat.IsPublic });
    }

    // Покинуть чат
    [HttpDelete("{chatId}/leave/{userId}")]
    public async Task<IActionResult> LeaveChat(int chatId, int userId)
    {
        var member = await _db.ChatMembers.FirstOrDefaultAsync(m => m.ChatId == chatId && m.UserId == userId);
        if (member == null) return NotFound();
        _db.ChatMembers.Remove(member);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("{chatId}/messages")]
    public async Task<IActionResult> GetMessages(int chatId)
    {
        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId).Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                m.Id,
                m.Text,
                m.SentAt,
                m.SenderId,
                SenderName = m.Sender!.Nickname ?? m.Sender.Username,
                SenderAvatar = m.Sender.AvatarUrl,
                m.IsEncrypted
            })
            .ToListAsync();
        return Ok(messages);
    }
}

public class CreateChatRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsChannel { get; set; }
    public bool IsPublic { get; set; }
    public int? AdminId { get; set; }
    public List<int> MemberIds { get; set; } = new();
}
public class InviteRequest { public int UserId { get; set; } }
public class UpdateChatRequest { public string? Name { get; set; } public string? Description { get; set; } public bool? IsPublic { get; set; } }
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;
using ChatServer.Models;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuthController(AppDbContext db) => _db = db;

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return BadRequest("Пользователь уже существует");

        var user = new User
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.Username });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Неверный логин или пароль");

        return Ok(new { user.Id, user.Username });
    }

    // GET /api/auth/users
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users.Select(u => new { u.Id, u.Username }).ToListAsync();
        return Ok(users);
    }
}

// GET /api/auth/chats/{userId}
// отдельный контроллер для чатов
public class AuthRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
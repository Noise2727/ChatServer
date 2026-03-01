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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return BadRequest("Логин уже занят");

        var user = new User
        {
            Username = req.Username,
            Nickname = string.IsNullOrEmpty(req.Nickname) ? req.Username : req.Nickname,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.Username, user.Nickname, user.AvatarUrl, user.Description });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Неверный логин или пароль");

        return Ok(new { user.Id, user.Username, user.Nickname, user.AvatarUrl, user.Description });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.Username, u.Nickname, u.AvatarUrl, u.Description })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.Username, user.Nickname, user.AvatarUrl, user.Description });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(req.Nickname)) user.Nickname = req.Nickname;
        if (req.Description != null) user.Description = req.Description;
        if (req.AvatarUrl != null) user.AvatarUrl = req.AvatarUrl;

        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.Username, user.Nickname, user.AvatarUrl, user.Description });
    }
}

public class AuthRequest { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
public class RegisterRequest { public string Username { get; set; } = ""; public string Password { get; set; } = ""; public string? Nickname { get; set; } }
public class UpdateProfileRequest { public string? Nickname { get; set; } public string? Description { get; set; } public string? AvatarUrl { get; set; } }
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;
using ChatServer.Hubs;

var builder = WebApplication.CreateBuilder(args);
// Добавь в самое начало перед builder
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=chat.db"));

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Автосоздание БД
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

app.UseCors();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
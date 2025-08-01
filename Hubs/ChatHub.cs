using Microsoft.AspNetCore.SignalR;
using ChatCompartido.Models; // tu DTO de mensaje para el cliente
using ChatCompartido.Data;   // EF
using Microsoft.EntityFrameworkCore;

namespace ChatCompartido.Hubs;

public class ChatHub : Hub
{
    private readonly ChatDbContext _db;

    public ChatHub(ChatDbContext db)
    {
        _db = db;
    }

    // Enviar texto o GIF
    public async Task SendMessage(string user, string text, string? gifUrl)
    {
        var entity = new ChatMessageEntity
        {
            User = user,
            Text = string.IsNullOrWhiteSpace(text) ? null : text,
            GifUrl = string.IsNullOrWhiteSpace(gifUrl) ? null : gifUrl,
            Timestamp = DateTimeOffset.Now
        };

        _db.Messages.Add(entity);
        await _db.SaveChangesAsync();

        // Mapear a DTO del cliente
        var dto = new ChatMessage
        {
            User = entity.User,
            Text = entity.Text ?? "",
            GifUrl = entity.GifUrl,
            Timestamp = entity.Timestamp
        };

        await Clients.All.SendAsync("ReceiveMessage", dto);
    }

    public override async Task OnConnectedAsync()
    {
        // Últimos 50 (orden cronológico)
        var last = await _db.Messages
            .OrderByDescending(m => m.Id)
            .Take(50)
            .OrderBy(m => m.Id)
            .ToListAsync();

        foreach (var m in last)
        {
            var dto = new ChatMessage
            {
                User = m.User,
                Text = m.Text ?? "",
                GifUrl = m.GifUrl,
                Timestamp = m.Timestamp
            };
            await Clients.Caller.SendAsync("ReceiveMessage", dto);
        }

        // Mensaje del sistema
        await Clients.Caller.SendAsync("ReceiveMessage",
            ChatMessage.System("¡Bienvenido!"));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.All.SendAsync("ReceiveMessage",
            ChatMessage.System($"Salió un usuario ({Context.ConnectionId[..6]}…)"));
        await base.OnDisconnectedAsync(exception);
    }
}

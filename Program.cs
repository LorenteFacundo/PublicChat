using ChatCompartido.Components;   // App (Razor components)
using ChatCompartido.Hubs;         // ChatHub (SignalR)
using ChatCompartido.Data;         // ChatDbContext (EF Core)
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Servicios
// ---------------------------------------------------------------------

// Blazor interactivo (Server)
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpClient para componentes (@inject HttpClient Http) y para IHttpClientFactory
builder.Services.AddHttpClient();

// EF Core + SQLite (historial de chat)
// Lee "ConnectionStrings:ChatDb" si existe; si no, usa "chat.db" en la raíz
builder.Services.AddDbContext<ChatDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("ChatDb")
        ?? "Data Source=chat.db"));

// ---------------------------------------------------------------------
// App
// ---------------------------------------------------------------------
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Montar la app de Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ---------------------------------------------------------------------
// Endpoints adicionales
// ---------------------------------------------------------------------

// 🔌 SignalR Hub del chat
app.MapHub<ChatHub>("/hubs/chat");

// 🔎 Endpoint de búsqueda de GIFs (GIPHY)
// GET /api/gifs?q=texto&limit=20
app.MapGet("/api/gifs", async (
    IHttpClientFactory http,
    IConfiguration cfg,
    string q,
    int? limit,
    CancellationToken ct) =>
{
    var apiKey = cfg["GIPHY_API_KEY"];
    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.BadRequest(new { error = "Falta GIPHY_API_KEY" });

    var top = limit is > 0 and <= 50 ? limit.Value : 20;

    var url =
        $"https://api.giphy.com/v1/gifs/search" +
        $"?api_key={Uri.EscapeDataString(apiKey)}" +
        $"&q={Uri.EscapeDataString(q)}" +
        $"&limit={top}" +
        $"&rating=g&lang=es";

    var client = http.CreateClient();
    using var resp = await client.GetAsync(url, ct);
    if (!resp.IsSuccessStatusCode)
    {
        var err = await resp.Content.ReadAsStringAsync(ct);
        return Results.Problem($"Giphy error: {(int)resp.StatusCode} {err}");
    }

    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));

    // Selecciona una imagen de preview y una URL animada para enviar al chat
    var list = new List<object>();
    foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
    {
        if (!item.TryGetProperty("images", out var images)) continue;

        string? preview =
            images.TryGetProperty("preview_gif", out var pg) ? pg.GetProperty("url").GetString() :
            images.TryGetProperty("fixed_height_small_still", out var st) ? st.GetProperty("url").GetString() :
            images.TryGetProperty("fixed_height_small", out var sm) ? sm.GetProperty("url").GetString() :
            null;

        string? gif =
            images.TryGetProperty("downsized", out var d) ? d.GetProperty("url").GetString() :
            images.TryGetProperty("original", out var o) ? o.GetProperty("url").GetString() :
            images.TryGetProperty("fixed_height", out var fh) ? fh.GetProperty("url").GetString() :
            null;

        if (gif is not null)
            list.Add(new { previewUrl = preview ?? gif, url = gif });
    }

    return Results.Ok(list);
});

// ---------------------------------------------------------------------
// Inicializar / migrar la base de datos al arrancar
// ---------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    // Si ya creaste una migración (recomendado):
    //   dotnet ef migrations add InitChat
    //   dotnet ef database update
    // esto aplicará las migraciones pendientes:
    db.Database.Migrate();

    // Si preferís evitar migraciones en desarrollo, podrías usar:
    // db.Database.EnsureCreated();
    // (pero luego migrarás manualmente si cambias el modelo)
}

app.Run();

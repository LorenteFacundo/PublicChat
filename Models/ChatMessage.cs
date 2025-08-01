namespace ChatCompartido.Models;

public sealed class ChatMessage
{
    public string User { get; set; } = "";
    public string Text { get; set; } = "";
    public string? GifUrl { get; set; }  // <- URL del GIF (opcional)
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    public static ChatMessage System(string text) =>
        new ChatMessage { User = "sistema", Text = text, Timestamp = DateTimeOffset.Now };
}

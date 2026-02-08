using System.Text.Json;

namespace Sayr.Tray;

internal sealed class AppSettings
{
    public string BackendUrl { get; set; } = "http://localhost:8080";
    public string Model { get; set; } = "whisper-1";
    public string Hotkey { get; set; } = "Alt+Space";

    public static AppSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            var defaults = new AppSettings();
            var json = JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            return defaults;
        }

        var text = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppSettings>(text) ?? new AppSettings();
    }
}

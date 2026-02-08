namespace Sayr.Tray;

internal static class Logger
{
    public static event Action<string>? MessageLogged;

    public static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        MessageLogged?.Invoke(line);
    }
}

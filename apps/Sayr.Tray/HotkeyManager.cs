using System.Runtime.InteropServices;

namespace Sayr.Tray;

internal sealed class HotkeyManager : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private readonly int _hotkeyId;
    private bool _registered;

    public event EventHandler? HotkeyPressed;

    public HotkeyManager(int hotkeyId)
    {
        _hotkeyId = hotkeyId;
        CreateHandle(new CreateParams());
    }

    public void Register(string hotkey)
    {
        if (_registered)
        {
            Unregister();
        }

        ParseHotkey(hotkey, out var modifiers, out var key);
        _registered = RegisterHotKey(Handle, _hotkeyId, modifiers, key);
        if (!_registered)
        {
            throw new InvalidOperationException($"Failed to register hotkey: {hotkey}");
        }
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(Handle, _hotkeyId);
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == _hotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
        GC.SuppressFinalize(this);
    }

    private static void ParseHotkey(string hotkey, out uint modifiers, out uint key)
    {
        modifiers = 0;
        key = 0;

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "alt":
                    modifiers |= ModAlt;
                    break;
                case "ctrl":
                case "control":
                    modifiers |= ModControl;
                    break;
                case "shift":
                    modifiers |= ModShift;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModWin;
                    break;
                default:
                    if (Enum.TryParse(part, true, out Keys parsed))
                    {
                        key = (uint)parsed;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unknown hotkey token: {part}");
                    }
                    break;
            }
        }

        if (key == 0)
        {
            throw new InvalidOperationException($"Hotkey is missing a key: {hotkey}");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

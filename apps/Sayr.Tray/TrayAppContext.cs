namespace Sayr.Tray;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly HotkeyManager _hotkeyManager;
    private readonly AudioRecorder _recorder;
    private readonly TranscriptionClient _transcriptionClient;
    private readonly AppSettings _settings;
    private readonly CancellationTokenSource _cts = new();
    private bool _busy;

    public TrayAppContext()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _settings = AppSettings.Load(settingsPath);

        _recorder = new AudioRecorder();
        _hotkeyManager = new HotkeyManager(1);
        _hotkeyManager.HotkeyPressed += (_, _) => ToggleRecordingAsync();
        _hotkeyManager.Register(_settings.Hotkey);

        _transcriptionClient = new TranscriptionClient(new HttpClient(), _settings.BackendUrl, _settings.Model);

        var menu = new ContextMenuStrip();
        var toggleItem = new ToolStripMenuItem("Start recording", null, (_, _) => ToggleRecordingAsync());
        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitThread());
        menu.Items.Add(toggleItem);
        menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Text = "Sayr",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    private async void ToggleRecordingAsync()
    {
        if (_busy)
        {
            return;
        }

        try
        {
            _busy = true;
            if (!_recorder.IsRecording)
            {
                _recorder.Start();
                UpdateTray("Recording... (Alt+Space)", "Stop recording");
                return;
            }

            UpdateTray("Transcribing...", "Transcribing...");
            var wav = await _recorder.StopAsync();
            if (wav.Length == 0)
            {
                UpdateTray("Sayr", "Start recording");
                return;
            }

            var text = await _transcriptionClient.TranscribeAsync(wav, _cts.Token);
            if (!string.IsNullOrWhiteSpace(text))
            {
                TextInjector.PasteText(text.Trim());
            }
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(4000, "Sayr error", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            UpdateTray("Sayr", "Start recording");
            _busy = false;
        }
    }

    private void UpdateTray(string tooltip, string toggleText)
    {
        if (_trayIcon.ContextMenuStrip?.Items.Count > 0)
        {
            if (_trayIcon.ContextMenuStrip.Items[0] is ToolStripMenuItem item)
            {
                item.Text = toggleText;
            }
        }

        _trayIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
    }

    protected override void ExitThreadCore()
    {
        _cts.Cancel();
        _hotkeyManager.Dispose();
        _recorder.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.ExitThreadCore();
    }
}

namespace Sayr.Tray;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly HotkeyManager _hotkeyManager;
    private readonly AudioRecorder _recorder;
    private readonly TranscriptionClient _transcriptionClient;
    private readonly AppSettings _settings;
    private readonly OverlayForm _overlay;
    private readonly LogForm _logForm;
    private readonly CancellationTokenSource _cts = new();
    private bool _busy;
    private DateTime _lastHotkeyUtc = DateTime.MinValue;
    private IntPtr _targetWindow;

    public TrayAppContext()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _settings = AppSettings.Load(settingsPath);

        _recorder = new AudioRecorder();
        _hotkeyManager = new HotkeyManager(1);
        _hotkeyManager.HotkeyPressed += (_, _) => ToggleRecordingAsync();
        try
        {
            _hotkeyManager.Register(_settings.Hotkey);
            Logger.Log($"Registered hotkey: {_settings.Hotkey}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Hotkey registration failed: {ex.Message}");
            MessageBox.Show(
                $"Failed to register hotkey '{_settings.Hotkey}'.\n\n{ex.Message}\n\n" +
                "The app will still run, but the hotkey won't work. " +
                "Close any app using that hotkey or update appsettings.json.",
                "Sayr hotkey error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        _transcriptionClient = new TranscriptionClient(new HttpClient(), _settings.BackendUrl, _settings.Model);
        _overlay = new OverlayForm();
        _logForm = new LogForm();
        Logger.MessageLogged += _logForm.AppendLine;

        var menu = new ContextMenuStrip();
        var logsItem = new ToolStripMenuItem("Show logs", null, (_, _) => ToggleLogs());
        var toggleItem = new ToolStripMenuItem("Start recording", null, (_, _) => ToggleRecordingAsync());
        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitThread());
        menu.Items.Add(logsItem);
        menu.Items.Add(new ToolStripSeparator());
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
        var now = DateTime.UtcNow;
        if (now - _lastHotkeyUtc < TimeSpan.FromMilliseconds(300))
        {
            return;
        }
        _lastHotkeyUtc = now;

        if (_busy)
        {
            return;
        }

        try
        {
            _busy = true;
            if (!_recorder.IsRecording)
            {
                Logger.Log("Recording started.");
                _targetWindow = GetForegroundWindow();
                _recorder.Start();
                UpdateTray($"Recording... ({_settings.Hotkey})", "Stop recording");
                _overlay.ShowStatus("Recording...", $"{_settings.Hotkey} to stop");
                _busy = false;
                return;
            }

            Logger.Log("Recording stopped. Transcribing...");
            UpdateTray("Transcribing...", "Transcribing...");
            _overlay.ShowStatus("Transcribing...", "Please wait");
            var wav = await _recorder.StopAsync();
            if (wav.Length == 0)
            {
                Logger.Log("No audio captured.");
                UpdateTray("Sayr", "Start recording");
                _overlay.HideStatus();
                return;
            }

            var text = await _transcriptionClient.TranscribeAsync(wav, _cts.Token);
            if (!string.IsNullOrWhiteSpace(text))
            {
                Logger.Log("Transcription complete. Injecting text.");
                TextInjector.PasteText(text.Trim(), _targetWindow);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error: {ex.Message}");
            _overlay.HideStatus();
            _trayIcon.ShowBalloonTip(4000, "Sayr error", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            if (!_recorder.IsRecording)
            {
                UpdateTray("Sayr", "Start recording");
                _overlay.HideStatus();
            }
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
        _overlay.Dispose();
        _logForm.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.ExitThreadCore();
    }

    private void ToggleLogs()
    {
        if (_logForm.Visible)
        {
            _logForm.Hide();
            return;
        }

        _logForm.Show();
        _logForm.BringToFront();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}

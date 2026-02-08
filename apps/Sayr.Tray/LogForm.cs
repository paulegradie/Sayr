namespace Sayr.Tray;

internal sealed class LogForm : Form
{
    private readonly TextBox _text;

    public LogForm()
    {
        Text = "Sayr Logs";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(720, 420);

        _text = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9F, FontStyle.Regular)
        };

        Controls.Add(_text);
    }

    public void AppendLine(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLine(line));
            return;
        }

        _text.AppendText(line + Environment.NewLine);
    }
}

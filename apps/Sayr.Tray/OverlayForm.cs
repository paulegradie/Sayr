using System.Drawing.Drawing2D;

namespace Sayr.Tray;

internal sealed class OverlayForm : Form
{
    private readonly Label _title;
    private readonly Label _subtitle;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(20, 20, 20);
        ForeColor = Color.White;
        Opacity = 0.9;
        Size = new Size(320, 72);

        _title = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White
        };

        _subtitle = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopCenter,
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.Gainsboro
        };

        Controls.Add(_subtitle);
        Controls.Add(_title);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExNoActivate | WsExToolWindow;
            return cp;
        }
    }

    public void ShowStatus(string title, string subtitle)
    {
        _title.Text = title;
        _subtitle.Text = subtitle;
        PositionOverlay();
        if (!Visible)
        {
            Show();
        }
    }

    public void HideStatus()
    {
        if (Visible)
        {
            Hide();
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        PositionOverlay();
        ApplyRoundedCorners(12);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyRoundedCorners(12);
    }

    private void PositionOverlay()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        var working = screen.WorkingArea;
        Location = new Point(
            working.Left + (working.Width - Width) / 2,
            working.Bottom - Height - 48);
    }

    private void ApplyRoundedCorners(int radius)
    {
        var path = new GraphicsPath();
        var rect = new Rectangle(0, 0, Width, Height);
        var diameter = radius * 2;

        path.StartFigure();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        Region = new Region(path);
    }
}

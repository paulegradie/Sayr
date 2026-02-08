using System.Runtime.InteropServices;

namespace Sayr.Tray;

internal static class TextInjector
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;
    private const ushort VkControl = 0x11;
    private const ushort VkV = 0x56;

    public static void PasteText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var previous = Clipboard.GetText();
        try
        {
            Clipboard.SetText(text);
            Thread.Sleep(30);
            SendCtrlV();
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous))
            {
                Clipboard.SetText(previous);
            }
        }
    }

    private static void SendCtrlV()
    {
        var inputs = new INPUT[4];
        inputs[0] = new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VkControl } } };
        inputs[1] = new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VkV } } };
        inputs[2] = new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VkV, dwFlags = KeyeventfKeyup } } };
        inputs[3] = new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VkControl, dwFlags = KeyeventfKeyup } } };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}

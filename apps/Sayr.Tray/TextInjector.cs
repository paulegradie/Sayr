using System.Runtime.InteropServices;

namespace Sayr.Tray;

internal static class TextInjector
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;
    private const ushort VkControl = 0x11;
    private const ushort VkV = 0x56;

    public static void PasteText(string text, IntPtr targetWindow)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var focused = FocusTargetWindow(targetWindow);
        Logger.Log(focused ? "Target window focused for paste." : "Target window focus not confirmed.");

        var pasted = false;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                Thread.Sleep(40);
                if (!SendCtrlV())
                {
                    Logger.Log("SendInput did not send all inputs. Trying SendKeys.");
                    TrySendKeysPaste();
                }
                Logger.Log("Sent Ctrl+V.");
                pasted = true;
                break;
            }
            catch (Exception ex)
            {
                Logger.Log($"Clipboard set failed (attempt {attempt}): {ex.Message}");
                Thread.Sleep(60);
            }
        }

        if (!pasted)
        {
            Logger.Log("Paste did not complete (clipboard unavailable).");
        }
    }

    private static bool SendCtrlV()
    {
        var inputs = new INPUT[4];
        inputs[0] = new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VkControl } } };
        inputs[1] = new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VkV } } };
        inputs[2] = new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VkV, dwFlags = KeyeventfKeyup } } };
        inputs[3] = new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = VkControl, dwFlags = KeyeventfKeyup } } };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
        {
            Logger.Log($"SendInput sent {sent}/{inputs.Length} inputs. Win32Error={Marshal.GetLastWin32Error()}");
            return false;
        }

        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    private static bool FocusTargetWindow(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero)
        {
            return false;
        }

        var current = GetForegroundWindow();
        if (current == targetWindow)
        {
            return true;
        }

        var currentThread = GetCurrentThreadId();
        var targetThread = GetWindowThreadProcessId(targetWindow, out _);
        var attached = false;

        if (currentThread != targetThread)
        {
            attached = AttachThreadInput(currentThread, targetThread, true);
        }

        var ok = SetForegroundWindow(targetWindow);
        if (!ok)
        {
            Logger.Log("Failed to set foreground window.");
        }
        else
        {
            Logger.Log("Foreground window focused.");
        }

        if (attached)
        {
            AttachThreadInput(currentThread, targetThread, false);
        }

        return ok;
    }

    private static void TrySendKeysPaste()
    {
        try
        {
            SendKeys.SendWait("^v");
        }
        catch (Exception ex)
        {
            Logger.Log($"SendKeys paste failed: {ex.Message}");
        }
    }

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

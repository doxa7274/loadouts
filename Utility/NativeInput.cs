using steam.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace steam.Utility
{
    public static class NativeInput
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int x, int y);

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const uint INPUT_MOUSE = 0;
        const uint INPUT_KEYBOARD = 1;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint KEYEVENTF_KEYUP = 0x0002;

        public static bool IsDestiny2Focused()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                return Process.GetProcessById((int)pid).ProcessName.Equals("Destiny2", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static Color GetScreenPixel(int x, int y)
        {
            var hdc = GetDC(IntPtr.Zero);
            try
            {
                var pixel = GetPixel(hdc, x, y);
                return Color.FromArgb(
                    (int)(pixel & 0x000000FF),
                    (int)((pixel & 0x0000FF00) >> 8),
                    (int)((pixel & 0x00FF0000) >> 16));
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        public static bool MatchesFlexibleHex(Color color, string pattern)
        {
            var hex = $"{color.R:X2}{color.G:X2}{color.B:X2}";
            if (pattern.Length != 6) return false;
            for (int i = 0; i < 6; i++)
            {
                if (pattern[i] != 'E' && pattern[i] != 'D' && pattern[i] != 'F' && char.ToUpperInvariant(pattern[i]) != char.ToUpperInvariant(hex[i]))
                    return false;
            }
            return true;
        }

        public static void MoveMouse(int x, int y) => SetCursorPos(x, y);

        public static Point GetMousePosition()
        {
            GetCursorPos(out var p);
            return new Point(p.X, p.Y);
        }

        public static void LeftClick()
        {
            SendInput(1, new[] { new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN } } } }, Marshal.SizeOf<INPUT>());
            SendInput(1, new[] { new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } } } }, Marshal.SizeOf<INPUT>());
        }

        public static void SendKey(Keys key, bool keyUp = false)
        {
            SendInput(1, new[]
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)key,
                            dwFlags = keyUp ? KEYEVENTF_KEYUP : 0
                        }
                    }
                }
            }, Marshal.SizeOf<INPUT>());
        }

        public static void TapKey(Keys key)
        {
            SendKey(key, false);
            SendKey(key, true);
        }

        public static void SendKeybind(IEnumerable<Keycode> bind)
        {
            var keys = new List<Keys>();
            foreach (var code in bind)
            {
                if (TryToKeys(code, out var k))
                    keys.Add(k);
            }

            foreach (var k in keys)
                SendKey(k, false);
            Thread.Sleep(10);
            foreach (var k in keys)
                SendKey(k, true);
        }

        static bool TryToKeys(Keycode code, out Keys key)
        {
            if (Enum.TryParse(code.ToString().Replace("VK_", ""), true, out key))
                return true;
            key = default;
            return false;
        }

        public static void PreciseSleep(double ms)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < ms)
                Thread.SpinWait(50);
        }

        public static async Task PreciseSleepAsync(double ms)
        {
            await Task.Run(() => PreciseSleep(ms));
        }
    }
}

using steam.Models;
using steam.Utility;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace steam.Loadouts
{
    public static class LoadoutCoordinateCapture
    {
        public enum CaptureMode { None, SingleSlot, BatchAll }

        public static CaptureMode Mode { get; private set; } = CaptureMode.None;
        public static int TargetProfile { get; private set; }
        public static int TargetSlot { get; private set; }
        public static int BatchIndex { get; private set; }
        public static bool WaitingForShiftZero { get; private set; }

        public static event Action<string> StatusChanged;

        public static void StartSingleSlot(int profileIndex, int slotIndex)
        {
            Cancel();
            TargetProfile = profileIndex;
            TargetSlot = slotIndex;
            Mode = CaptureMode.SingleSlot;
            Notify($"Hover loadout slot {slotIndex + 1} and press F2");
        }

        public static void StartBatch(int profileIndex)
        {
            Cancel();
            TargetProfile = profileIndex;
            BatchIndex = 0;
            WaitingForShiftZero = true;
            Mode = CaptureMode.BatchAll;
            Notify("Press Shift+0, then click loadout slots 1 through 20");
        }

        public static void Cancel()
        {
            bool wasActive = Mode != CaptureMode.None;
            Mode = CaptureMode.None;
            WaitingForShiftZero = false;
            BatchIndex = 0;
            if (wasActive)
                MainWindow.Instance?.Dispatcher.BeginInvoke(() =>
                {
                    try { LoadoutsWindow.RefreshSlotsIfOpen(); } catch { }
                });
        }

        public static void HandleKeyPress(LinkedList<Keycode> keys)
        {
            if (Mode == CaptureMode.None) return;

            if (Mode == CaptureMode.SingleSlot && keys.Contains(Keycode.VK_F2))
            {
                CaptureAtCursor(TargetProfile, TargetSlot);
                Cancel();
                return;
            }

            if (Mode == CaptureMode.BatchAll)
            {
                if (WaitingForShiftZero && keys.Contains(Keycode.VK_LSHIFT) && keys.Contains(Keycode.VK_0))
                {
                    WaitingForShiftZero = false;
                    BatchIndex = 0;
                    Notify("Click loadout slot 1");
                    return;
                }

                if (!WaitingForShiftZero && keys.Any(k => k == Keycode.VK_LMB) == false)
                {
                    // batch uses mouse click handler
                }
            }
        }

        public static void HandleMouseDown()
        {
            if (Mode != CaptureMode.BatchAll || WaitingForShiftZero) return;
            if (BatchIndex >= 20)
            {
                Cancel();
                Notify("All 20 slots captured");
                return;
            }

            CaptureAtCursor(TargetProfile, BatchIndex);
            BatchIndex++;
            if (BatchIndex < 20)
                Notify($"Captured slot {BatchIndex}. Click slot {BatchIndex + 1}");
            else
            {
                Cancel();
                Notify("All 20 slots captured");
            }
        }

        static void CaptureAtCursor(int profileIndex, int slotIndex)
        {
            var pos = NativeInput.GetMousePosition();
            var profile = LoadoutsModule.Settings.Profiles[Math.Clamp(profileIndex, 0, 4)];
            profile.Slots[slotIndex].X = pos.X;
            profile.Slots[slotIndex].Y = pos.Y;
            profile.Slots[slotIndex].Active = true;
            LoadoutsModule.SaveConfig();
            Notify($"Slot {slotIndex + 1}: ({pos.X}, {pos.Y})");
        }

        static void Notify(string msg)
        {
            Logger.Info($"Loadouts capture: {msg}");
            StatusChanged?.Invoke(msg);
            if (Mode == CaptureMode.None)
                return;
        }
    }
}

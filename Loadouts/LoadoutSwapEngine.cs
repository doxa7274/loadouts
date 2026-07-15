using steam.Interception.Modules;
using steam.Models;
using steam.Utility;

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace steam.Loadouts
{
    public static class LoadoutSwapEngine
    {
        static volatile bool _running;

        public static bool IsRunning => _running;

        public static async Task RunSwapAsync(LoadoutsConfig config, LoadoutProfileSettings profile, LoadoutPortMode? overrideMode = null)
        {
            if (_running) return;
            if (!NativeInput.IsDestiny2Focused()) return;

            _running = true;
            try
            {
                await Task.Run(() => ExecuteSwap(config, profile, overrideMode ?? profile.PortMode));
            }
            finally
            {
                _running = false;
            }
        }

        public static async Task RunQuickSwapAsync(LoadoutsConfig config, LoadoutProfileSettings profile, int loadoutIndex)
        {
            if (_running || loadoutIndex < 1 || loadoutIndex > 20) return;
            if (!NativeInput.IsDestiny2Focused()) return;

            var slot = profile.Slots[loadoutIndex - 1];
            if (slot.X == 0 && slot.Y == 0) return;

            _running = true;
            try
            {
                await Task.Run(() =>
                {
                    NativeInput.MoveMouse(slot.X, slot.Y);
                    NativeInput.PreciseSleep(50);
                    NativeInput.LeftClick();
                });
            }
            finally
            {
                _running = false;
            }
        }

        static void ExecuteSwap(LoadoutsConfig config, LoadoutProfileSettings profile, LoadoutPortMode mode)
        {
            bool use3074 = mode is LoadoutPortMode.Pve3074 or LoadoutPortMode.Both;
            bool use27k = mode is LoadoutPortMode.Pvp27k or LoadoutPortMode.Both;
            bool useNone = mode == LoadoutPortMode.None;

            int ending = Math.Clamp(profile.EndingLoadout, 1, 20);
            var screen = config.Screen;

            int enabledCount = 0;
            int damagePosition = 0;
            for (int i = 0; i < 20; i++)
            {
                if (profile.SelectedLoadouts[i])
                {
                    enabledCount++;
                    if (i + 1 < ending)
                        damagePosition++;
                }
            }

            if (enabledCount == 0) return;

            var endingSlot = profile.Slots[ending - 1];
            int loadDualX = endingSlot.X;
            int loadDualY = endingSlot.Y;

            long trueStart = Environment.TickCount64;

            if (use3074 && PveModule.OutboundKeybind.Any())
                NativeInput.SendKeybind(PveModule.OutboundKeybind);

            bool dlPressed = false;
            if (profile.Enable3074Dl && (use3074 || use27k) && Config.GetNamed("PVE").Keybind.Any())
            {
                NativeInput.SendKeybind(Config.GetNamed("PVE").Keybind);
                dlPressed = true;
            }

            if (use27k && PvpModule.OutboundKeybind.Any())
                NativeInput.SendKeybind(PvpModule.OutboundKeybind);

            bool openedInventory = OpenInventoryIfNeeded(screen, profile);

            if (profile.AutoDisableBuffering && use3074)
                SetBuffering(false);

            NavigateToLoadoutScreen(screen, profile, openedInventory);

            long start = Environment.TickCount64;
            int swapTime = useNone ? profile.SwapDurationNoneMs :
                use27k && !use3074 ? profile.SwapDuration27kMs : profile.SwapDurationMs;

            long timeEnd = start + swapTime;
            long time27End = start + profile.SwapDuration27kMs;
            long time74Fix = ComputeTimeFix(start, swapTime, profile.DelayBetweenLoadoutsMs, enabledCount, profile.EnhancedDelay && screen.UseEnhancedPixels);

            if (enabledCount <= 2)
                time74Fix = timeEnd;

            NativeInput.TapKey(Keys.Left);
            int stolbNum = 0;
            bool alreadyExtended = false;
            bool k27Sent = false;
            int debugCount = 0;

            double halfPoint = enabledCount / 2.0;
            int direction = damagePosition < halfPoint ? 1 : -1;

            while (Environment.TickCount64 < timeEnd)
            {
                for (int idx = 0; idx < 20; idx++)
                {
                    int loadNum = direction == 1 ? idx + 1 : 20 - idx;
                    if (!profile.SelectedLoadouts[loadNum - 1]) continue;

                    var slot = profile.Slots[loadNum - 1];
                    if (slot.X == loadDualX && slot.Y == loadDualY && Environment.TickCount64 >= time74Fix)
                        continue;

                    if (profile.ShowSwapTimer)
                    {
                        double remain = Math.Max(0, (timeEnd - Environment.TickCount64) / 1000.0);
                        // tooltip omitted in integrated version
                    }

                    NativeInput.MoveMouse(slot.X, slot.Y);

                    if (!profile.EnhancedDelay || !screen.UseEnhancedPixels)
                    {
                        NativeInput.PreciseSleep(profile.DelayBetweenLoadoutsMs / 2.0);
                        NativeInput.LeftClick();
                        debugCount++;
                        NativeInput.PreciseSleep(profile.DelayBetweenLoadoutsMs / 2.0);
                    }
                    else
                    {
                        int col = ((loadNum - 1) % 4) + 1;
                        if (stolbNum != col)
                            stolbNum = col;
                        else
                            NativeInput.PreciseSleep(profile.DelayBetweenLoadoutsMs / 2.0);

                        int testX = screen.LoadTestX[Math.Clamp(col - 1, 0, 3)];
                        bool clicked = false;
                        for (int attempt = 0; attempt < 4; attempt++)
                        {
                            var c1 = NativeInput.GetScreenPixel(testX, screen.LoadTestYBottom);
                            var c2 = NativeInput.GetScreenPixel(testX, screen.LoadTestYMid);
                            var c3 = NativeInput.GetScreenPixel(testX, screen.LoadTestYMid2);
                            var c4 = NativeInput.GetScreenPixel(testX, screen.LoadTestYTop);
                            if (IsBright(c1) || IsBright(c2) || IsBright(c3) || IsBright(c4) || attempt >= 3)
                            {
                                NativeInput.LeftClick();
                                debugCount++;
                                clicked = true;
                                break;
                            }
                        }
                        if (!clicked)
                            NativeInput.LeftClick();
                    }

                    if (Environment.TickCount64 >= timeEnd)
                        break;
                }

                if (!k27Sent && use27k && Environment.TickCount64 >= time27End && time27End < timeEnd)
                {
                    if (PvpModule.OutboundKeybind.Any())
                        NativeInput.SendKeybind(PvpModule.OutboundKeybind);
                    k27Sent = true;
                    if (!use3074 && profile.Enable3074Dl && dlPressed && Config.GetNamed("PVE").Keybind.Any())
                        NativeInput.SendKeybind(Config.GetNamed("PVE").Keybind);
                }

                if (Environment.TickCount64 >= timeEnd)
                {
                    NativeInput.MoveMouse(loadDualX, loadDualY);
                    foreach (var d in new[] { 50, 50, 50, 10, 1, 1, 1, 1, 1 })
                    {
                        NativeInput.PreciseSleep(d);
                        NativeInput.LeftClick();
                    }
                    break;
                }
            }

            if (profile.CloseInventoryAfter)
                NativeInput.TapKey(Keys.F1);

            NativeInput.PreciseSleep(profile.UntickDelayMs);

            if (use27k && !k27Sent && PvpModule.OutboundKeybind.Any())
                NativeInput.SendKeybind(PvpModule.OutboundKeybind);

            if (use3074 && PveModule.OutboundKeybind.Any())
                NativeInput.SendKeybind(PveModule.OutboundKeybind);

            if (profile.Enable3074Dl && dlPressed && Config.GetNamed("PVE").Keybind.Any())
                NativeInput.SendKeybind(Config.GetNamed("PVE").Keybind);

            if (profile.AutoDisableBuffering && use3074)
                SetBuffering(true);

            Logger.Info($"Loadouts: swap finished in {Environment.TickCount64 - trueStart}ms ({enabledCount} slots)");
        }

        static long ComputeTimeFix(long start, int swapTime, int delay, int enabledCount, bool enhanced)
        {
            if (enabledCount <= 2) return start + swapTime;
            if (enhanced || delay <= 37)
                return start + swapTime - (37L * enabledCount * 3);
            if (delay <= 50)
                return start + swapTime - (delay * enabledCount * 2L);
            if (delay <= 80)
                return start + swapTime - (long)(delay * enabledCount * 1.5);
            return start + swapTime - (delay * enabledCount);
        }

        static bool OpenInventoryIfNeeded(LoadoutScreenCalibration screen, LoadoutProfileSettings profile)
        {
            for (int i = 0; i < 10; i++)
            {
                var inv1 = NativeInput.GetScreenPixel(screen.InvColorX, screen.InvColorY);
                var inv2 = NativeInput.GetScreenPixel(screen.InvColor2X, screen.InvColor2Y);
                var load = NativeInput.GetScreenPixel(screen.LoadColorX, screen.LoadColorY);

                if (!NativeInput.MatchesFlexibleHex(inv1, "EEEEEE") && !NativeInput.MatchesFlexibleHex(inv2, "EEEEEE")
                    && !NativeInput.MatchesFlexibleHex(load, "EEEEEE") && !NativeInput.MatchesFlexibleHex(load, "DDDDDD"))
                {
                    if (profile.InventoryBind.Any())
                        NativeInput.SendKeybind(profile.InventoryBind);
                    else
                        NativeInput.TapKey(Keys.F1);

                    NativeInput.SendKey(Keys.RButton, true);
                    Thread.Sleep(100);
                    return true;
                }
            }
            return false;
        }

        static void NavigateToLoadoutScreen(LoadoutScreenCalibration screen, LoadoutProfileSettings profile, bool openedInventory)
        {
            if (profile.OpenLoadoutsBind.Any())
                NativeInput.SendKeybind(profile.OpenLoadoutsBind);

            if (openedInventory)
            {
                for (int i = 0; i < 40; i++)
                {
                    NativeInput.MoveMouse(50, Screen.PrimaryScreen.Bounds.Height / 2);
                    NativeInput.TapKey(Keys.Left);
                    NativeInput.LeftClick();
                    NativeInput.PreciseSleep(1);
                    var load = NativeInput.GetScreenPixel(screen.LoadColorX, screen.LoadColorY);
                    if (NativeInput.MatchesFlexibleHex(load, "EEEEEE") || NativeInput.MatchesFlexibleHex(load, "DDDDDD"))
                        break;
                }
            }
            else
            {
                NativeInput.TapKey(Keys.Left);
                NativeInput.MoveMouse(50, Screen.PrimaryScreen.Bounds.Height / 2);
                Thread.Sleep(100);
            }

            long waitStart = Environment.TickCount64;
            while (Environment.TickCount64 - waitStart < 500)
            {
                var load = NativeInput.GetScreenPixel(screen.LoadColorX, screen.LoadColorY);
                if (NativeInput.MatchesFlexibleHex(load, "EEEEEE") || NativeInput.MatchesFlexibleHex(load, "DDDDDD"))
                    break;
            }
        }

        static bool IsBright(Color c) =>
            NativeInput.MatchesFlexibleHex(c, "FFFFFF") || c.R > 240 && c.G > 240 && c.B > 240;

        static void SetBuffering(bool enabled)
        {
            if (PveModule.Buffer == enabled) return;
            PveModule.Buffer = enabled;
            Config.Save();
            MainWindow.Instance?.Dispatcher.BeginInvoke(() =>
            {
                try { MainWindow.Instance.PveBufferCB?.SetState(enabled); } catch { }
            });
        }
    }
}

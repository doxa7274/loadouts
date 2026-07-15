using steam.Loadouts;
using steam.Models;
using steam.Utility;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace steam.Interception.Modules
{
    public class LoadoutsModule : PacketModuleBase
    {
        public static LoadoutsConfig Settings { get; private set; } = new();
        static bool _swapsStarted;

        public LoadoutsModule() : base("Loadouts", false, null)
        {
            Icon = System.Windows.Application.Current.FindResource("Engram") as Geometry;
            Description = "Automated Destiny 2 loadout swapping with per-profile settings and pixel coordinates.";

            LoadConfig();
            ApplyScreenDefaults();

            KeyListener.KeysPressed += SwapBindHandler;
            KeyListener.KeysPressed += QuickSwapHandler;
            KeyListener.KeysPressed += CaptureHandler;
        }

        public static void LoadConfig()
        {
            var moduleSettings = Utility.Config.GetNamed("Loadouts").Settings;
            if (moduleSettings.TryGetValue("LoadoutsData", out var raw) && raw is JsonElement el)
            {
                try
                {
                    Settings = JsonSerializer.Deserialize<LoadoutsConfig>(el.GetRawText()) ?? new LoadoutsConfig();
                }
                catch
                {
                    Settings = new LoadoutsConfig();
                }
            }
            else
            {
                Settings = new LoadoutsConfig();
            }
        }

        public static void SaveConfig()
        {
            Utility.Config.GetNamed("Loadouts").Settings["LoadoutsData"] = JsonSerializer.SerializeToElement(Settings);
            Utility.Config.Save();
        }

        static void ApplyScreenDefaults()
        {
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(1920, 1080);
            LoadoutCalibrationDefaults.ApplyForScreen(Settings, bounds.Width, bounds.Height);
        }

        public override void Toggle()
        {
            IsActivated = !IsActivated;
        }

        void SwapBindHandler(LinkedList<Keycode> keys)
        {
            if (!KeybindChecks() || LoadoutSwapEngine.IsRunning || _swapsStarted) return;
            if (!NativeInput.IsDestiny2Focused()) return;

            for (int i = 0; i < Settings.Profiles.Length; i++)
            {
                var profile = Settings.Profiles[i];
                var bind = profile.SwapBind;
                if (!bind.Any() || keys.Count < bind.Count) continue;
                if (!bind.All(keys.Contains)) continue;

                _swapsStarted = true;
                latestTrigger = DateTime.Now;
                Logger.Info($"Loadouts: Profile {i + 1} swap triggered");
                Task.Run(async () =>
                {
                    try
                    {
                        await LoadoutSwapEngine.RunSwapAsync(Settings, profile);
                    }
                    finally
                    {
                        _swapsStarted = false;
                    }
                });
                return;
            }
        }

        void QuickSwapHandler(LinkedList<Keycode> keys)
        {
            if (!IsEnabled || !hooked || LoadoutSwapEngine.IsRunning) return;
            if (Utility.Config.Instance?.Settings.AltTabSupressKeybinds == true && !OverlayWindow.CheckGameFocus()) return;
            if (!NativeInput.IsDestiny2Focused()) return;

            for (int i = 0; i < Settings.QuickSwapBinds.Length; i++)
            {
                var bind = Settings.QuickSwapBinds[i];
                if (!bind.Any() || keys.Count < bind.Count) continue;
                if (!bind.All(keys.Contains)) continue;

                latestTrigger = DateTime.Now;
                var profile = Settings.Active;
                int loadoutNum = i + 1;
                Logger.Info($"Loadouts: Quick swap loadout {loadoutNum}");
                Task.Run(() => LoadoutSwapEngine.RunQuickSwapAsync(Settings, profile, loadoutNum));
                return;
            }
        }

        void CaptureHandler(LinkedList<Keycode> keys)
        {
            if (LoadoutCoordinateCapture.Mode == LoadoutCoordinateCapture.CaptureMode.None) return;

            if (LoadoutCoordinateCapture.Mode == LoadoutCoordinateCapture.CaptureMode.BatchAll
                && !LoadoutCoordinateCapture.WaitingForShiftZero
                && keys.Contains(Keycode.VK_LMB))
            {
                LoadoutCoordinateCapture.HandleMouseDown();
            MainWindow.Instance?.Dispatcher.BeginInvoke(() =>
            {
                try { LoadoutsWindow.RefreshSlotsIfOpen(); } catch { }
            });
            return;
            }

            LoadoutCoordinateCapture.HandleKeyPress(keys);
            if (LoadoutCoordinateCapture.Mode == LoadoutCoordinateCapture.CaptureMode.None)
                MainWindow.Instance?.Dispatcher.BeginInvoke(() =>
                {
                    try { LoadoutsWindow.RefreshSlotsIfOpen(); } catch { }
                });
        }
    }
}

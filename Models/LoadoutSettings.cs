using steam.Utility;

using System;
using System.Collections.Generic;

namespace steam.Models
{
    public enum LoadoutPortMode
    {
        Pve3074 = 0,
        Pvp27k = 1,
        Both = 2,
        None = 3
    }

    public class LoadoutSlotSettings
    {
        public bool Active { get; set; } = true;
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class LoadoutProfileSettings
    {
        public string Name { get; set; } = "Profile";
        public List<Keycode> SwapBind { get; set; } = new();
        public List<Keycode> InventoryBind { get; set; } = new();
        public List<Keycode> OpenLoadoutsBind { get; set; } = new();
        public LoadoutPortMode PortMode { get; set; } = LoadoutPortMode.Pve3074;
        public int SwapDurationMs { get; set; } = 5000;
        public int SwapDuration27kMs { get; set; } = 5000;
        public int SwapDurationNoneMs { get; set; } = 5000;
        public int DelayBetweenLoadoutsMs { get; set; } = 40;
        public int UntickDelayMs { get; set; } = 600;
        public bool Enable3074Dl { get; set; }
        public bool CloseInventoryAfter { get; set; }
        public bool AutoDisableBuffering { get; set; }
        public bool EnhancedDelay { get; set; } = true;
        public bool ShowSwapTimer { get; set; } = true;
        public int EndingLoadout { get; set; } = 1;
        public bool[] SelectedLoadouts { get; set; } = new bool[20];
        public LoadoutSlotSettings[] Slots { get; set; } = CreateDefaultSlots();

        static LoadoutSlotSettings[] CreateDefaultSlots()
        {
            var slots = new LoadoutSlotSettings[20];
            for (int i = 0; i < 20; i++)
                slots[i] = new LoadoutSlotSettings();
            return slots;
        }
    }

    public class LoadoutScreenCalibration
    {
        public int LoadColorX { get; set; }
        public int LoadColorY { get; set; }
        public int InvColorX { get; set; }
        public int InvColorY { get; set; }
        public int InvColor2X { get; set; }
        public int InvColor2Y { get; set; }
        public int TimerX { get; set; }
        public int TimerY { get; set; }
        public int[] LoadTestX { get; set; } = new int[4];
        public int LoadTestYBottom { get; set; }
        public int LoadTestYMid { get; set; }
        public int LoadTestYMid2 { get; set; }
        public int LoadTestYTop { get; set; }
        public bool UseEnhancedPixels { get; set; }
    }

    public class LoadoutsConfig
    {
        public int ActiveProfile { get; set; }
        public LoadoutProfileSettings[] Profiles { get; set; } = CreateProfiles();
        public List<Keycode>[] QuickSwapBinds { get; set; } = CreateQuickSwapBinds();
        public LoadoutScreenCalibration Screen { get; set; } = new();

        static LoadoutProfileSettings[] CreateProfiles()
        {
            var profiles = new LoadoutProfileSettings[5];
            for (int i = 0; i < 5; i++)
            {
                profiles[i] = new LoadoutProfileSettings
                {
                    Name = $"Profile {i + 1}"
                };
            }
            return profiles;
        }

        static List<Keycode>[] CreateQuickSwapBinds()
        {
            var binds = new List<Keycode>[20];
            for (int i = 0; i < 20; i++)
                binds[i] = new List<Keycode>();
            return binds;
        }

        public LoadoutProfileSettings Active => Profiles[Math.Clamp(ActiveProfile, 0, Profiles.Length - 1)];
    }
}

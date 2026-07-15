using steam.Models;

using System;
using System.Drawing;
using System.Windows.Forms;

namespace steam.Loadouts
{
    public static class LoadoutCalibrationDefaults
    {
        public static void ApplyForScreen(LoadoutsConfig config, int width, int height)
        {
            var screen = config.Screen;

            if (width == 1920 && height == 1080)
            {
                Apply1080pScreen(screen);
                screen.UseEnhancedPixels = true;
            }
            else if (width == 2560 && height == 1440)
            {
                Apply1440pScreen(screen);
                screen.UseEnhancedPixels = true;
            }
            else
            {
                ApplyScaledScreen(screen, width, height);
                screen.UseEnhancedPixels = false;
            }

            foreach (var profile in config.Profiles)
                ApplyProfileSlotsIfEmpty(profile, width, height);
        }

        public static void ApplyProfileSlotsIfEmpty(LoadoutProfileSettings profile, int width, int height)
        {
            if (HasCustomCoords(profile)) return;

            if (width == 1920 && height == 1080)
            {
                int[] xs = { 110, 210, 310, 410, 110, 210, 310, 410, 110, 210, 310, 410, 110, 210, 310, 410, 110, 210, 310, 410 };
                int[] ys = { 380, 380, 380, 380, 480, 480, 480, 480, 580, 580, 580, 580, 680, 680, 680, 680, 780, 780, 780, 780 };
                ApplySlotCoords(profile, xs, ys);
            }
            else
            {
                double m = height / 1080.0;
                double offset = (width - 1920.0 * m) / 2;
                for (int i = 0; i < 20; i++)
                {
                    profile.Slots[i].X = (int)((110 + (i % 4) * 100) * m + offset);
                    profile.Slots[i].Y = (int)((380 + (i / 4) * 100) * m);
                }
            }
        }

        static void Apply1080pScreen(LoadoutScreenCalibration s)
        {
            s.LoadColorX = 77; s.LoadColorY = 104;
            s.InvColorX = 960; s.InvColorY = 1035;
            s.InvColor2X = 960; s.InvColor2Y = 1014;
            s.TimerX = 73; s.TimerY = 820;
            s.LoadTestX = new[] { 220, 320, 420, 520 };
            s.LoadTestYBottom = 740; s.LoadTestYMid = 350; s.LoadTestYMid2 = 430; s.LoadTestYTop = 470;
        }

        static void Apply1440pScreen(LoadoutScreenCalibration s)
        {
            double m = 1440.0 / 1080.0;
            double offset = (2560 - 1920.0 * m) / 2;
            s.LoadColorX = 102; s.LoadColorY = 139;
            s.InvColorX = 1280; s.InvColorY = 1380;
            s.InvColor2X = 1280; s.InvColor2Y = 1351;
            s.TimerX = (int)(73 * m + offset); s.TimerY = (int)(820 * m);
            s.LoadTestX = new[] { (int)(220 * m + offset), (int)(320 * m + offset), (int)(420 * m + offset), (int)(520 * m + offset) };
            s.LoadTestYBottom = (int)(740 * m); s.LoadTestYMid = (int)(350 * m);
            s.LoadTestYMid2 = (int)(430 * m); s.LoadTestYTop = (int)(470 * m);
        }

        static void ApplyScaledScreen(LoadoutScreenCalibration s, int w, int h)
        {
            Apply1080pScreen(s);
            double m = h / 1080.0;
            double offset = (w - 1920.0 * m) / 2;
            s.TimerX = (int)(73 * m + offset);
            s.TimerY = (int)(820 * m);
        }

        static void ApplySlotCoords(LoadoutProfileSettings p, int[] xs, int[] ys)
        {
            for (int i = 0; i < 20; i++)
            {
                if (p.Slots[i].X == 0 && p.Slots[i].Y == 0)
                {
                    p.Slots[i].X = xs[i];
                    p.Slots[i].Y = ys[i];
                }
            }
        }

        public static bool HasCustomCoords(LoadoutProfileSettings profile)
        {
            foreach (var slot in profile.Slots)
            {
                if (slot.X != 0 || slot.Y != 0)
                    return true;
            }
            return false;
        }
    }
}

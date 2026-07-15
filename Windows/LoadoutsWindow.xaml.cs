using steam.Controls;
using steam.Interception;
using steam.Interception.Modules;
using steam.Loadouts;
using steam.Models;
using steam.Utility;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace steam
{
    public partial class LoadoutsWindow : Window
    {
        static LoadoutsWindow _instance;
        LoadoutsConfig Config => LoadoutsModule.Settings;
        LoadoutProfileSettings Profile => Config.Profiles[_profileIndex];
        int _profileIndex;
        readonly Dictionary<Button, List<Keycode>> _listening = new();
        readonly Dictionary<int, Checkbox> _loadoutChecks = new();
        readonly Dictionary<int, Checkbox> _slotActive = new();
        readonly Dictionary<int, Label> _slotCoords = new();
        readonly Dictionary<int, Button> _quickSwapBtns = new();
        readonly Dictionary<int, Label> _slotManagerLabels = new();
        List<Keycode> _activeBind;

        public static void RefreshSlotsIfOpen()
        {
            _instance?.RefreshProfile();
        }

        public static void ShowNear(Window owner)
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new LoadoutsWindow
                {
                    Owner = owner,
                    Left = owner.Left + owner.Width + 4,
                    Top = owner.Top
                };
            }
            _instance.Show();
            _instance.Activate();
        }

        Action<string> _captureHandler;

        LoadoutsWindow()
        {
            InitializeComponent();
            PortCombo.ItemsSource = new[] { "3074 UL", "27k UL", "Both", "No Port" };
            BuildLoadoutGrid();
            BuildSlotRows();
            BuildSlotManagerGrid();
            BuildQuickSwap();
            _captureHandler = s => Dispatcher.BeginInvoke(() => CaptureStatus.Content = s);
            LoadoutCoordinateCapture.StatusChanged += _captureHandler;
            _profileIndex = Config.ActiveProfile;
            RefreshProfile();
            RefreshEnabled();
        }

        void BuildLoadoutGrid()
        {
            for (int i = 1; i <= 20; i++)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                var cb = new Checkbox { Width = 18, Height = 18, Margin = new Thickness(0, 0, 4, 0) };
                int n = i;
                cb.Click += (_, _) => { Profile.SelectedLoadouts[n - 1] = cb.Checked; Save(); };
                panel.Children.Add(cb);
                panel.Children.Add(new Label
                {
                    Content = n.ToString(),
                    Foreground = (Brush)FindResource("TitleColor"),
                    FontFamily = (System.Windows.Media.FontFamily)FindResource("Kantumruy"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(-4, 0, 0, 0)
                });
                LoadoutGrid.Children.Add(panel);
                _loadoutChecks[i] = cb;
            }
        }

        void BuildSlotRows()
        {
            for (int i = 1; i <= 20; i++)
            {
                var grid = new Grid { Margin = new Thickness(0, 1, 0, 1) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });

                var active = new Checkbox { Width = 18, Height = 18, HorizontalAlignment = HorizontalAlignment.Left };
                int n = i;
                active.Click += (_, _) => { Profile.Slots[n - 1].Active = active.Checked; Save(); };
                Grid.SetColumn(active, 0);
                grid.Children.Add(active);
                _slotActive[i] = active;

                var coords = new Label
                {
                    Foreground = (Brush)FindResource("TitleColor"),
                    FontFamily = (System.Windows.Media.FontFamily)FindResource("Kantumruy"),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(coords, 1);
                grid.Children.Add(coords);
                _slotCoords[i] = coords;

                var edit = new Button { Text = "Edit", Height = 20, MaxWidth = 48, Margin = new Thickness(2, 0, 0, 0) };
                edit.Click += (_, _) =>
                {
                    LoadoutCoordinateCapture.StartSingleSlot(_profileIndex, n - 1);
                    CaptureStatus.Content = $"Hover slot {n} and press F2";
                };
                Grid.SetColumn(edit, 2);
                grid.Children.Add(edit);

                SlotPanel.Children.Add(grid);
            }
        }

        void BuildSlotManagerGrid()
        {
            for (int i = 1; i <= 20; i++)
            {
                var grid = new Grid { Margin = new Thickness(2) };
                grid.Background = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF));
                grid.CornerRadius = new CornerRadius(4);

                var label = new Label
                {
                    Content = $"Slot {i}\n--",
                    Foreground = (Brush)FindResource("TitleColor"),
                    FontFamily = (System.Windows.Media.FontFamily)FindResource("Kantumruy"),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                grid.Children.Add(label);
                SlotManagerGrid.Children.Add(grid);
                _slotManagerLabels[i] = label;
            }
        }

        void BuildQuickSwap()
        {
            for (int i = 1; i <= 20; i++)
            {
                var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                grid.Children.Add(new Label
                {
                    Content = $"Loadout {i}",
                    Foreground = (Brush)FindResource("TitleColor"),
                    FontFamily = (System.Windows.Media.FontFamily)FindResource("Kantumruy"),
                    HorizontalAlignment = HorizontalAlignment.Left
                });
                var btn = new Button
                {
                    Text = FormatBind(Config.QuickSwapBinds[i - 1]),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    MaxWidth = 140
                };
                int n = i;
                btn.Click += (_, _) => BeginListen(btn, Config.QuickSwapBinds[n - 1]);
                grid.Children.Add(btn);
                QuickSwapPanel.Children.Add(grid);
                _quickSwapBtns[i] = btn;
            }
        }

        void ProfileTab_Click(object sender, RoutedEventArgs e)
        {
            FlushProfileFromUi();
            _profileIndex = int.Parse(((Button)sender).Text) - 1;
            Config.ActiveProfile = _profileIndex;
            EnsureProfileDefaults(Profile);
            RefreshProfile();
            Save();
        }

        void EnsureProfileDefaults(LoadoutProfileSettings p)
        {
            var b = System.Windows.Forms.Screen.PrimaryScreen?.Bounds ?? new Rectangle(1920, 1080);
            LoadoutCalibrationDefaults.ApplyProfileSlotsIfEmpty(p, b.Width, b.Height);
        }

        public void RefreshProfile()
        {
            HighlightProfileTab();
            SwapBindBtn.Text = FormatBind(Profile.SwapBind);
            InventoryBindBtn.Text = FormatBind(Profile.InventoryBind);
            OpenLoadoutsBindBtn.Text = FormatBind(Profile.OpenLoadoutsBind);
            PortCombo.SelectedIndex = (int)Profile.PortMode;
            UpdateDurationField();
            DelayBox.Text = Profile.DelayBetweenLoadoutsMs.ToString();
            UntickBox.Text = Profile.UntickDelayMs.ToString();
            EndingBox.Text = Profile.EndingLoadout.ToString();
            DlCB.SetState(Profile.Enable3074Dl);
            CloseInvCB.SetState(Profile.CloseInventoryAfter);
            AutoBufCB.SetState(Profile.AutoDisableBuffering);
            EnhancedCB.SetState(Profile.EnhancedDelay);
            TimerCB.SetState(Profile.ShowSwapTimer);

            for (int i = 1; i <= 20; i++)
            {
                _loadoutChecks[i].SetState(Profile.SelectedLoadouts[i - 1]);
                _slotActive[i].SetState(Profile.Slots[i - 1].Active);
                var s = Profile.Slots[i - 1];
                _slotCoords[i].Content = s.X == 0 && s.Y == 0 ? "not set" : $"{s.X}, {s.Y}";
                
                // Update Slot Manager Grid
                _slotManagerLabels[i].Content = s.X == 0 && s.Y == 0 ? $"Slot {i}\nNot Set" : $"Slot {i}\n{s.X}, {s.Y}";
            }
        }

        void HighlightProfileTab()
        {
            foreach (var i in Enumerable.Range(1, 5))
            {
                var b = (Button)FindName($"Prof{i}");
                b.Background = i - 1 == _profileIndex
                    ? (Brush)FindResource("AccentColor")
                    : new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF));
            }
        }

        void RefreshEnabled()
        {
            var mod = InterceptionManager.GetModule("Loadouts") as LoadoutsModule;
            EnabledCB.SetState(mod?.IsEnabled == true);
        }

        void EnabledCB_Click(object sender, RoutedEventArgs e)
        {
            var mod = InterceptionManager.GetModule("Loadouts") as LoadoutsModule;
            if (mod == null) return;
            if (EnabledCB.Checked) mod.StartListening();
            else mod.StopListening();
            Utility.Config.GetNamed("Loadouts").Enabled = EnabledCB.Checked;
            Utility.Config.Save();
        }

        void PortCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (PortCombo.SelectedIndex < 0) return;
            Profile.PortMode = (LoadoutPortMode)PortCombo.SelectedIndex;
            UpdateDurationField();
            Save();
        }

        void UpdateDurationField()
        {
            DurationBox.Text = Profile.PortMode switch
            {
                LoadoutPortMode.Pvp27k => Profile.SwapDuration27kMs.ToString(),
                LoadoutPortMode.None => Profile.SwapDurationNoneMs.ToString(),
                _ => Profile.SwapDurationMs.ToString()
            };
        }

        void Numeric_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb || !int.TryParse(tb.Text, out int v)) return;
            if (tb == DelayBox) Profile.DelayBetweenLoadoutsMs = Math.Clamp(v, 1, 9999);
            else if (tb == UntickBox) Profile.UntickDelayMs = Math.Clamp(v, 0, 99999);
            else if (tb == DurationBox)
            {
                v = Math.Clamp(v, 100, 120000);
                switch (Profile.PortMode)
                {
                    case LoadoutPortMode.Pvp27k: Profile.SwapDuration27kMs = v; break;
                    case LoadoutPortMode.None: Profile.SwapDurationNoneMs = v; break;
                    default: Profile.SwapDurationMs = v; break;
                }
            }
            Save();
        }

        void Ending_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(EndingBox.Text, out int v))
            {
                Profile.EndingLoadout = Math.Clamp(v, 1, 20);
                EndingBox.Text = Profile.EndingLoadout.ToString();
                Save();
            }
        }

        void OptionCB_Click(object sender, RoutedEventArgs e)
        {
            Profile.Enable3074Dl = DlCB.Checked;
            Profile.CloseInventoryAfter = CloseInvCB.Checked;
            Profile.AutoDisableBuffering = AutoBufCB.Checked;
            Profile.EnhancedDelay = EnhancedCB.Checked;
            Profile.ShowSwapTimer = TimerCB.Checked;
            Save();
        }

        void StartEditing_Click(object sender, RoutedEventArgs e)
        {
            LoadoutCoordinateCapture.StartBatch(_profileIndex);
            CaptureStatus.Content = "Press Shift+0, then click loadout slots 1 through 20";
        }

        void BatchCapture_Click(object sender, RoutedEventArgs e)
        {
            LoadoutCoordinateCapture.StartBatch(_profileIndex);
            BatchCaptureStatus.Content = "Press Shift+0, then click loadout slots 1 through 20";
        }

        void Keybind_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            List<Keycode> target = btn == SwapBindBtn ? Profile.SwapBind
                : btn == InventoryBindBtn ? Profile.InventoryBind
                : Profile.OpenLoadoutsBind;
            BeginListen(btn, target);
        }

        void BeginListen(Button btn, List<Keycode> target)
        {
            if (_listening.ContainsKey(btn))
            {
                EndListen(btn);
                Save();
                if (_quickSwapBtns.ContainsValue(btn))
                    btn.Text = FormatBind(target);
                else
                    RefreshProfile();
                return;
            }

            if (_listening.Count == 0)
            {
                InterceptionManager.Modules.ForEach(x => x.UnhookKeybind());
                KeyListener.KeysPressed += OnListenKeys;
            }

            _activeBind = target;
            _listening[btn] = target;
            btn.ButtonBorder.BorderThickness = new Thickness(1.75);
            btn.ButtonBorder.BorderBrush = Brushes.White;
            btn.ButtonBorder.Effect = new DropShadowEffect { ShadowDepth = 0, Color = Colors.White, BlurRadius = 8 };
            btn.Text = "Press keys...";
        }

        void EndListen(Button btn)
        {
            _listening.Remove(btn);
            btn.ButtonBorder.BorderThickness = new Thickness(0);
            btn.ButtonBorder.BorderBrush = Brushes.Transparent;
            btn.ButtonBorder.Effect = null;
            if (_listening.Count == 0)
            {
                KeyListener.KeysPressed -= OnListenKeys;
                InterceptionManager.Modules.ForEach(x => x.HookKeybind());
            }
        }

        void OnListenKeys(LinkedList<Keycode> keys)
        {
            if (_activeBind == null) return;
            if (keys.Count == 1 && keys.First.Value == Keycode.VK_LMB) return;

            _activeBind.Clear();
            if (keys.Count == 1 && keys.First.Value == Keycode.VK_ESC)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    foreach (var b in _listening.Keys.ToList())
                    {
                        b.Text = "No keybind";
                        EndListen(b);
                    }
                });
                return;
            }

            _activeBind.AddRange(keys);
            Dispatcher.BeginInvoke(() =>
            {
                foreach (var kv in _listening.ToList())
                    kv.Key.Text = FormatBind(kv.Value);
            });
        }

        void FlushProfileFromUi()
        {
            Numeric_LostFocus(DurationBox, null);
            Numeric_LostFocus(DelayBox, null);
            Numeric_LostFocus(UntickBox, null);
            Ending_LostFocus(EndingBox, null);
        }

        static string FormatBind(List<Keycode> bind) =>
            bind.Any() ? string.Join(" + ", bind.Select(x => x.ToString().Replace("VK_", ""))) : "No keybind";

        static void Save() => LoadoutsModule.SaveConfig();

        void Close_Click(object sender, RoutedEventArgs e)
        {
            FlushProfileFromUi();
            Save();
            foreach (var b in _listening.Keys.ToList()) EndListen(b);
            Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_captureHandler != null)
                LoadoutCoordinateCapture.StatusChanged -= _captureHandler;
            _instance = null;
            base.OnClosed(e);
        }
    }
}

using System;
using System.Windows;
using Solar.Properties;
using CoordinateSharp;
using System.Management;
using System.Threading;
using System.Windows.Controls;
using System.Linq;
using Microsoft.Win32;

namespace Solar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public SettingsWindow settings;

        static Mutex mutex = new Mutex(true, "LukeTaylor.Solar (6C80ED49-BCA4-4EF0-9FD3-9D9781C7A678)");

        private static System.Timers.Timer sunsetTimer;

        private static readonly string userID = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;

        private const string startupRegistryPath = @"Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string themeRegistryPath = @"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
        private static readonly string wmiRegistryPath = String.Format(@"{0}\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", userID);
        private static readonly string executableLocation = String.Format("\"{0}\"", System.Reflection.Assembly.GetEntryAssembly().Location);
        // private static string startupRegistryKeyPath = String.Format(@"{0}\\Software\\Microsoft\\Windows\\CurrentVersion\\Run", userID);

        private System.Windows.Forms.ToolStripMenuItem locationButton;

        private System.Windows.Forms.ToolStripMenuItem startupCheckbox;

        private System.Windows.Forms.ToolStripMenuItem systemChangeSunset;
        private System.Windows.Forms.ToolStripMenuItem appChangeSunset;
        private System.Windows.Forms.ToolStripMenuItem systemChangeQuickSwitch;
        private System.Windows.Forms.ToolStripMenuItem appChangeQuickSwitch;

        private System.Windows.Forms.ToolStripMenuItem manualMenu;
        private System.Windows.Forms.ToolStripMenuItem[] manualMenuItems = new System.Windows.Forms.ToolStripMenuItem[4];
        int currentlyCheckedManualItem;

        private System.Windows.Forms.ToolStripMenuItem lightModeCheckbox;
        private System.Windows.Forms.ToolStripMenuItem darkModeCheckbox;
        
        private System.Windows.Forms.NotifyIcon notifyIcon;

        private bool lastSunState;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            if (Settings.Default.FirstLaunch)
            {
                addToStartup();
                Settings.Default.FirstLaunch = false;
                Settings.Default.Save();
            }

            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Another instance of Solar is already running, so this one will quit.", "Solar is already running!");
                QuitApplication();   
            }
            else {
                mutex.ReleaseMutex();

                base.OnStartup(e);

                // addToStartup();

                notifyIcon = new System.Windows.Forms.NotifyIcon();
                notifyIcon.Icon = Solar.Properties.Resources.AppIcon;
                notifyIcon.ContextMenuStrip = CreateTrayMenu();
                notifyIcon.Visible = true;
                notifyIcon.DoubleClick += (s, args) => ToggleTheme();

                InitializeThemeData();
                SunsetCheck(true);

                sunsetTimer = new System.Timers.Timer();
                sunsetTimer.Interval = 15000; // Check every 15 seconds
                sunsetTimer.Elapsed += (s,args) => SunsetCheck();
                sunsetTimer.Enabled = true;

                WqlEventQuery query = new WqlEventQuery(String.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND Keypath = '{0}' AND (ValueName='SystemUsesLightTheme' OR ValueName='AppsUseLightTheme')", wmiRegistryPath));

                ManagementEventWatcher watcher = new ManagementEventWatcher(query);

                watcher.EventArrived += new EventArrivedEventHandler(RegistryKeyChanged);

                watcher.Start();
            }
            
        }

        private bool checkIsInStartup()
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser;
                RegistryKey subKey = registryKey.CreateSubKey(startupRegistryPath);
                string result = (string)subKey.GetValue("Solar");
                if (result != null && result == executableLocation) return true;
                else return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading key!");
                Console.WriteLine(e);
                return false;
            }
        }

        private void removeFromStartup()
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser;
                RegistryKey subKey = registryKey.CreateSubKey(startupRegistryPath);
                subKey.DeleteValue("Solar");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error deleting key!");
                Console.WriteLine(e);
            }
        }

        public void addToStartup()
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser;
                RegistryKey subKey = registryKey.CreateSubKey(startupRegistryPath);
                subKey.SetValue("Solar", executableLocation);
                Console.WriteLine("Entry successfully added!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error adding key!");
                Console.WriteLine(e);
            }
        }

        public void SetLocationBold(bool makeBold)
        {
            if (makeBold)
            {
                locationButton.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            }
            else
            {
                locationButton.Font = new System.Drawing.Font("Segoe UI", 9);
            }
            
        }

        /// <summary>
        /// Constructs Solar's system tray menu.
        /// </summary>
        /// <returns> The finished ContextMenuStrip</returns>
        private System.Windows.Forms.ContextMenuStrip CreateTrayMenu()
        {
            System.Windows.Forms.ContextMenuStrip trayMenu = new System.Windows.Forms.ContextMenuStrip();

            System.Drawing.Font boldFont = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);

            // Settings...
            /*System.Windows.Forms.ToolStripMenuItem settingsButton = new System.Windows.Forms.ToolStripMenuItem("Settings...");
            settingsButton.Click += (s, args) => OpenAuxWindow(SettingsWindow.Section.Settings);
            trayMenu.Items.Add(settingsButton);*/

            // Set Location...
            locationButton = new System.Windows.Forms.ToolStripMenuItem("Set Location...");
            if (!Settings.Default.HasSetLocation)
            {
                locationButton.Font = boldFont;
            }
            locationButton.Click += (s, args) => OpenAuxWindow(SettingsWindow.Section.Location);
            trayMenu.Items.Add(locationButton);

            // About ...
            System.Windows.Forms.ToolStripMenuItem aboutButton = new System.Windows.Forms.ToolStripMenuItem(String.Format("About Solar (v{0})...", Settings.Default.Version));
            aboutButton.Click += (s, args) => OpenAuxWindow(SettingsWindow.Section.About);
            trayMenu.Items.Add(aboutButton);

            // --------
            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            startupCheckbox = new System.Windows.Forms.ToolStripMenuItem("Launch at startup");
            startupCheckbox.Click += (s, args) => SetupCheckboxHandler();
            startupCheckbox.Checked = checkIsInStartup();
            trayMenu.Items.Add(startupCheckbox);

            // --------
            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // Change at sunset...
            System.Windows.Forms.ToolStripMenuItem sunsetMenu = new System.Windows.Forms.ToolStripMenuItem("Change at sunset...");
            systemChangeSunset = new System.Windows.Forms.ToolStripMenuItem("System Theme");
            systemChangeSunset.Click += (s, args) => HandleSunsetCategoryCheckbox(ThemeCategory.System);
            systemChangeSunset.Checked = Settings.Default.SunsetChangesSystem;
            appChangeSunset = new System.Windows.Forms.ToolStripMenuItem("App Themes");
            appChangeSunset.Click += (s, args) => HandleSunsetCategoryCheckbox(ThemeCategory.App);
            appChangeSunset.Checked = Settings.Default.SunsetChangesApp;
            sunsetMenu.DropDownItems.Add(systemChangeSunset);
            sunsetMenu.DropDownItems.Add(appChangeSunset);
            trayMenu.Items.Add(sunsetMenu);

            // Change with quick switch...
            System.Windows.Forms.ToolStripMenuItem quickSwitchMenu = new System.Windows.Forms.ToolStripMenuItem("Change with quick switch...");
            systemChangeQuickSwitch = new System.Windows.Forms.ToolStripMenuItem("System Theme");
            systemChangeQuickSwitch.Click += (s, args) => HandleQuickSwitchCategoryCheckbox(ThemeCategory.System);
            systemChangeQuickSwitch.Checked = Settings.Default.QuickSwitchChangesSystem;
            appChangeQuickSwitch = new System.Windows.Forms.ToolStripMenuItem("App Themes");
            appChangeQuickSwitch.Click += (s, args) => HandleQuickSwitchCategoryCheckbox(ThemeCategory.App);
            appChangeQuickSwitch.Checked = Settings.Default.QuickSwitchChangesSystem;
            quickSwitchMenu.DropDownItems.Add(systemChangeQuickSwitch);
            quickSwitchMenu.DropDownItems.Add(appChangeQuickSwitch);
            trayMenu.Items.Add(quickSwitchMenu);

            // --------
            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // Manual...
            manualMenu = new System.Windows.Forms.ToolStripMenuItem("Manual...");
            //System.Windows.Forms.ToolStripMenuItem[] manualMenuItems = new System.Windows.Forms.ToolStripMenuItem[4];
            /*manualMenu.DropDownItems.Add("🖥: ◇, 🗔: ◇");
            manualMenu.DropDownItems.Add("🖥: ◇, 🗔: ◆");
            manualMenu.DropDownItems.Add("🖥: ◆, 🗔: ◇");
            manualMenu.DropDownItems.Add("🖥: ◆, 🗔: ◆");*/
            Theme[] lightDarkIndicators = new Theme[2] {Theme.Light, Theme.Dark };
            int index = 0;
            foreach (Theme sysTheme in lightDarkIndicators)
            {
                foreach (Theme appTheme in lightDarkIndicators)
                {
                    System.Windows.Forms.ToolStripMenuItem temp = new System.Windows.Forms.ToolStripMenuItem(String.Format("Sys: {0}, App: {1}", sysTheme == Theme.Light ? "◇":"◆", appTheme == Theme.Light ? "◇" : "◆"));
                    manualMenuItems[index] = temp;
                    temp.Click += (s, args) =>
                    {
                        UpdateThemeIfNecessary(ThemeCategory.System, sysTheme);
                        UpdateThemeIfNecessary(ThemeCategory.App, appTheme);
                    };
                    manualMenu.DropDownItems.Add(temp);
                    index++;
                }
            }
            currentlyCheckedManualItem = -1;
            trayMenu.Items.Add(manualMenu);

            // --------
            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // Light
            lightModeCheckbox = new System.Windows.Forms.ToolStripMenuItem("Light");
            lightModeCheckbox.Click += (s, args) => QuickSwitchTheme(Theme.Light);
            // _lightModeCheckbox.Checked = currentTheme == Theme.Light;
            trayMenu.Items.Add(lightModeCheckbox);
            // Dark
            darkModeCheckbox = new System.Windows.Forms.ToolStripMenuItem("Dark");
            darkModeCheckbox.Click += (s, args) => QuickSwitchTheme(Theme.Dark);
            // _darkModeCheckbox.Checked = currentTheme == Theme.Dark;
            trayMenu.Items.Add(darkModeCheckbox);
            EvaluateQuickSwitchEligibility();

            // --------
            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // Exit
            trayMenu.Items.Add("Exit").Click += (s, args) => QuitApplication();

            return trayMenu;
        }

        /// <summary>
        /// Cleanly quits Solar
        /// </summary>
        private void QuitApplication()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            Current.Shutdown();
        }

        /// <summary>
        /// Deals with changes to themes outside of Solar and keeps consistency in the UI
        /// </summary>
        public void RegistryKeyChanged(object sender, EventArrivedEventArgs e)
        {
            Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)(() =>
            {

                InitializeThemeData();
               /* _lightModeCheckbox.Checked = currentTheme == Theme.Light;
                _darkModeCheckbox.Checked = currentTheme == Theme.Dark;*/
            }));
            
        }

        private void OpenAuxWindow(SettingsWindow.Section section)
        {
            if (settings == null)
            {
                settings = new SettingsWindow();
            }
            settings.OpenSection(section);
            settings.Show();
            settings.Activate();
        }

        /// <summary>
        /// Checks whether the sun is up in the user's set location.
        /// </summary>
        /// <returns>Returns whether the sun is up or not.</returns>
        private bool IsSunUp()
        {
            return new Coordinate(Settings.Default.Latitude, Settings.Default.Longitude, DateTime.UtcNow).CelestialInfo.IsSunUp;
        }

        /// <summary>
        /// Checks for a change in the state of the sun (sunset or sunrise) and changes theme accordingly.
        /// </summary>
        /// <param name="firstTime">
        /// Optional parameter to bypass the sunset state change requirement.
        /// </param>
        public void SunsetCheck(bool firstTime = false)
        {
            if (!Settings.Default.HasSetLocation) return;
            bool sunIsUp = IsSunUp();
            if (sunIsUp != lastSunState || firstTime)
            {
                if (sunIsUp) SunsetSwitchTheme(Theme.Light);
                else SunsetSwitchTheme(Theme.Dark);
                lastSunState = sunIsUp;
            }
        }

        private void HandleSunsetCategoryCheckbox(ThemeCategory category)
        {
            bool checkRemoved = false;
            if (category == ThemeCategory.System)
            {
                bool currentValue = systemChangeSunset.Checked;
                Settings.Default.SunsetChangesSystem = !currentValue;
                systemChangeSunset.Checked = !currentValue;
                checkRemoved = currentValue;
            }
            else if (category == ThemeCategory.App)
            {
                bool currentValue = appChangeSunset.Checked;
                Settings.Default.SunsetChangesApp = !currentValue;
                appChangeSunset.Checked = !currentValue;
                checkRemoved = currentValue;
            }
            Settings.Default.Save();
            if (!checkRemoved) UpdateThemeIfNecessary(category, lastSunState ? Theme.Light : Theme.Dark);
        }
        private void EvaluateQuickSwitchEligibility()
        {
            if (!Settings.Default.QuickSwitchChangesApp && !Settings.Default.QuickSwitchChangesSystem)
            {
                lightModeCheckbox.Enabled = false;
                darkModeCheckbox.Enabled = false;
            }
            else if (!lightModeCheckbox.Enabled && (Settings.Default.QuickSwitchChangesApp || Settings.Default.QuickSwitchChangesSystem))
            {
                lightModeCheckbox.Enabled = true;
                darkModeCheckbox.Enabled = true;
            }
        }
        private void HandleQuickSwitchCategoryCheckbox(ThemeCategory category)
        {
            if (category == ThemeCategory.System)
            {
                bool currentValue = systemChangeQuickSwitch.Checked;
                Settings.Default.QuickSwitchChangesSystem = !currentValue;
                systemChangeQuickSwitch.Checked = !currentValue;
            }
            else if (category == ThemeCategory.App)
            {
                bool currentValue = appChangeQuickSwitch.Checked;
                Settings.Default.QuickSwitchChangesApp = !currentValue;
                appChangeQuickSwitch.Checked = !currentValue;
            }
            Settings.Default.Save();
            QuickSwitchTheme(currentDisplayTheme);
            EvaluateQuickSwitchEligibility();
        }
        private void SetupCheckboxHandler()
        {
            if (startupCheckbox.Checked)
            {
                if (checkIsInStartup())
                {
                    removeFromStartup();
                }
                startupCheckbox.Checked = false;
            }
            else
            {
                addToStartup();
                startupCheckbox.Checked = true;
            }
        }
    }
}

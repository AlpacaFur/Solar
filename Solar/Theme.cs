using System;
using System.Windows;
using Solar.Properties;
using Microsoft.Win32;

namespace Solar
{
    public partial class App : Application
    {
        private enum Theme
        {
            Dark = 0,
            Light = 1,
            Unset = -1
        };
        private enum ThemeCategory
        {
            System = 0,
            App = 1
        };

        // private Theme currentTheme;
        private Theme currentSystemTheme = Theme.Unset;
        private Theme currentAppTheme = Theme.Unset;
        private Theme currentDisplayTheme = Theme.Unset;

        // Settings.Default.SunsetChangesAppTheme
        // Settings.Default.SunsetChangesSystemTheme

        private void InitializeThemeData()
        {
            currentSystemTheme = GetSystemTheme();
            currentAppTheme = GetAppTheme();
            UpdateDisplayTheme();
        }

        private void SunsetSwitchTheme(Theme theme)
        {
            if (Settings.Default.SunsetChangesApp)
            {
                UpdateThemeIfNecessary(ThemeCategory.App, theme);
            }
            if (Settings.Default.SunsetChangesSystem)
            {
                UpdateThemeIfNecessary(ThemeCategory.System, theme);
            }
        }

        private void QuickSwitchTheme(Theme theme)
        {
            if (Settings.Default.QuickSwitchChangesApp)
            {
                UpdateThemeIfNecessary(ThemeCategory.App, theme);
            }
            if (Settings.Default.QuickSwitchChangesSystem)
            {
                UpdateThemeIfNecessary(ThemeCategory.System, theme);
            }
        }

        private void ToggleTheme()
        {
            Theme themeToSet = currentDisplayTheme == Theme.Light ? Theme.Dark : Theme.Light;
            QuickSwitchTheme(themeToSet);
        }

        private void UpdateThemeIfNecessary(ThemeCategory category, Theme theme)
        {
            if (category == ThemeCategory.App && theme != currentAppTheme)
            {
                currentAppTheme = theme;
                UpdateDisplayTheme();
                SetRegistryTheme(ThemeCategory.App, theme);
            }
            if (category == ThemeCategory.System && theme != currentSystemTheme)
            {
                currentSystemTheme = theme;
                UpdateDisplayTheme();
                SetRegistryTheme(ThemeCategory.System, theme);
            }
        }

        private void UpdateDisplayTheme()
        {
            Theme calculatedTheme = GetDisplayTheme();
            if (currentDisplayTheme != calculatedTheme)
            {
                currentDisplayTheme = calculatedTheme;
                lightModeCheckbox.Checked = currentDisplayTheme == Theme.Light;
                darkModeCheckbox.Checked = currentDisplayTheme == Theme.Dark;
            }
            int index;
            if (currentSystemTheme == Theme.Light && currentAppTheme == Theme.Light)
            {
                index = 0;
            }
            else if (currentSystemTheme == Theme.Light && currentAppTheme == Theme.Dark)
            {
                index = 1;

            }
            else if (currentSystemTheme == Theme.Dark && currentAppTheme == Theme.Light)
            {
                index = 2;
            }
            else // Dark && Dark
            {
                index = 3;
            }
            if (index != currentlyCheckedManualItem)
            {
                if (currentlyCheckedManualItem != -1)
                {
                    manualMenuItems[currentlyCheckedManualItem].Checked = false;
                }
                manualMenuItems[index].Checked = true;
                currentlyCheckedManualItem = index;
            }
        }

        private Theme GetDisplayTheme()
        {
            if (Settings.Default.QuickSwitchChangesApp && Settings.Default.QuickSwitchChangesSystem)
            {
                return currentAppTheme;
            }
            else if (Settings.Default.QuickSwitchChangesApp)
            {
                return currentAppTheme;
            }
            else if (Settings.Default.QuickSwitchChangesSystem)
            {
                return currentSystemTheme;
            }
            else
            {
                return currentAppTheme;
            }
        }

        private Theme GetSystemTheme () => GetTheme(ThemeCategory.System);

        private Theme GetAppTheme () => GetTheme(ThemeCategory.App);

        private void SetRegistryTheme(ThemeCategory category, Theme theme)
        {
            String key;
            if (category == ThemeCategory.System)
            {
                key = "SystemUsesLightTheme";
            }
            else if (category == ThemeCategory.App)
            {
                key = "AppsUseLightTheme";
            }
            else
            {
                throw new Exception("Invalid theme category!");
            }
            try
            {
                RegistryKey registryKey = Registry.Users;
                RegistryKey subKey = registryKey.CreateSubKey(wmiRegistryPath);
                subKey.SetValue(key, (int)theme);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error writing key!");
                Console.WriteLine(e);
            }
        }

        private Theme GetTheme(ThemeCategory category)
        {
            string key;
            if (category == ThemeCategory.App)
            {
                key = "AppsUseLightTheme";
            }
            else // (category == ThemeCategory.System)
            {
                key = "SystemUsesLightTheme";
            }
            try
            {
                RegistryKey registryKey = Registry.Users;
                RegistryKey subKey = registryKey.CreateSubKey(wmiRegistryPath);
                int result = (int)subKey.GetValue(key);
                return (Theme)result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading key!");
                Console.WriteLine(e);
                return Theme.Dark;
            }
        }
    }
}

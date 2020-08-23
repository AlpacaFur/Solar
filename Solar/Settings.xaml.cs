using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using CoordinateSharp;
using Solar.Properties;

namespace Solar
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

        private readonly App app = ((App)Application.Current);

        public enum Section
        {
            Location = 0,
            About = 1
        };

        public SettingsWindow()
        {
            this.Loaded += Window_Loaded;
            this.Closing += Window_Closing;
            InitializeComponent();
            VersionInfoText.Text = String.Format("Version {0} ({1})", Settings.Default.Version, Settings.Default.VersionEra);

            try
            {
                Coordinate c = Coordinate.Parse("12°34'56\"N 65°43'21\"W");
                Console.WriteLine(c.Latitude);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.ToString());
            }
        }

        public void OpenSection(Section section)
        {
            Tabs.SelectedIndex = (int)section;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Rect desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;

            DisplayCurrentCoords();
        }

        private void DisplayCurrentCoords()
        {
            if (Settings.Default.HasSetLocation)
            {
                Coordinate coordinate = new Coordinate(Settings.Default.Latitude, Settings.Default.Longitude);
                CurrentLatLabel.Text = String.Format("Latitude: {0}", coordinate.Latitude.ToDouble());
                CurrentLongLabel.Text = String.Format("Longitude: {0}", coordinate.Longitude.ToDouble());
            }
            else
            {
                CurrentLatLabel.Text = "Latitude: Unset";
                CurrentLongLabel.Text = "Longitude: Unset";
            }
            
        }

        private void OpenLink(object s, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void AttemptSubmit()
        {
            CoordinateErrorMessage.Visibility = Visibility.Hidden;
            string coords = LatLongBox.Text;
            Coordinate coordinate;
            if (Coordinate.TryParse(coords, out coordinate))
            {
                if (!Settings.Default.HasSetLocation)
                {
                    Settings.Default.HasSetLocation = true;
                    app.SetLocationBold(false);
                    Settings.Default.Save();
                }
                LatLongBox.Text = "";
                Settings.Default.Latitude = coordinate.Latitude.ToDouble();
                Settings.Default.Longitude = coordinate.Longitude.ToDouble();
                Settings.Default.Save();
                DisplayCurrentCoords();
                app.SunsetCheck(true);
            }
            else
            {
                CoordinateErrorMessage.Visibility = Visibility.Visible;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AttemptSubmit();
        }

        private void ResetButton(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reset();
            app.SetLocationBold(true);
            this.Closing -= Window_Closing;
            this.Close();
            app.settings = null;
            app.addToStartup();
        }

        private void CheckIfEnterPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AttemptSubmit();
            }
        }
    }
}

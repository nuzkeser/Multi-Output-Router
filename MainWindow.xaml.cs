using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NAudio.CoreAudioApi;
using System.Collections.Generic;

namespace MultiOutputRouter
{
    public partial class MainWindow : Window
    {
        private MMDeviceEnumerator _deviceEnumerator = new MMDeviceEnumerator();
        private AudioEngine _audioEngine = new AudioEngine();
        private bool _isRouting = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadDevices();
        }

        private void LoadDevices()
        {
            var allDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
            
            var validDevices = new List<MMDevice>();
            bool hasVirtualCable = false;

            foreach (var d in allDevices)
            {
                try
                {
                    string name = d.FriendlyName; 
                    validDevices.Add(d);
                    if (name.Contains("CABLE") || name.Contains("VB-Audio"))
                    {
                        hasVirtualCable = true;
                    }
                }
                catch { } 
            }

            DriverInstallBanner.Visibility = hasVirtualCable ? Visibility.Collapsed : Visibility.Visible;

            SourceComboBox.SelectionChanged -= SourceComboBox_SelectionChanged;
            SourceComboBox.ItemsSource = validDevices;
            if (validDevices.Count > 0) SourceComboBox.SelectedIndex = 0;
            SourceComboBox.SelectionChanged += SourceComboBox_SelectionChanged;

            RenderDestinations();
        }

        private void SourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderDestinations();
        }

        private void RenderDestinations()
        {
            DestinationsPanel.Children.Clear();
            var validDevices = SourceComboBox.ItemsSource as List<MMDevice>;
            var sourceDevice = SourceComboBox.SelectedItem as MMDevice;

            if (validDevices == null) return;

            foreach (var device in validDevices)
            {
                if (sourceDevice != null && device.ID == sourceDevice.ID)
                {
                    continue; // Hide the device that is selected as source
                }

                var container = new StackPanel { Margin = new Thickness(0, 0, 0, 20), Tag = device };
                
                var checkBox = new CheckBox
                {
                    Content = device.FriendlyName,
                    Tag = device
                };
                
                var volumePanel = new Grid { Margin = new Thickness(25, 0, 0, 0) };
                volumePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                volumePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) }); 
                volumePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) }); 

                var volumeSlider = new Slider
                {
                    Minimum = 0.0,
                    Maximum = 1.0,
                    Value = 1.0,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = device.ID,
                    ToolTip = "Adjust volume"
                };

                var volumeText = new TextBox
                {
                    Text = "100",
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0,0,0,1),
                    BorderBrush = System.Windows.Media.Brushes.DimGray,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Width = 32,
                    FontSize = 13,
                    Tag = volumeSlider
                };

                var percentLabel = new TextBlock
                {
                    Text = "%",
                    Foreground = System.Windows.Media.Brushes.DimGray,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FontSize = 12
                };

                volumeSlider.ValueChanged += (s, e) =>
                {
                    if (!volumeText.IsFocused) 
                    {
                        volumeText.Text = $"{(int)(e.NewValue * 100)}";
                    }
                    _audioEngine.SetDeviceVolume((string)((Slider)s).Tag, (float)e.NewValue);
                };

                volumeText.LostFocus += (s, e) => ApplyVolumeText(volumeText, volumeSlider);
                volumeText.KeyDown += (s, e) => 
                { 
                    if (e.Key == System.Windows.Input.Key.Enter) 
                    {
                        ApplyVolumeText(volumeText, volumeSlider); 
                        System.Windows.Input.Keyboard.ClearFocus();
                    }
                };

                Grid.SetColumn(volumeSlider, 0);
                Grid.SetColumn(volumeText, 1);
                Grid.SetColumn(percentLabel, 2);
                
                volumePanel.Children.Add(volumeSlider);
                volumePanel.Children.Add(volumeText);
                volumePanel.Children.Add(percentLabel);

                container.Children.Add(checkBox);
                container.Children.Add(volumePanel);

                DestinationsPanel.Children.Add(container);
            }
        }

        private void ApplyVolumeText(TextBox txt, Slider slider)
        {
            if (int.TryParse(txt.Text, out int val))
            {
                if (val < 0) val = 0;
                if (val > 100) val = 100;
                txt.Text = val.ToString();
                slider.Value = val / 100.0;
            }
            else
            {
                txt.Text = $"{(int)(slider.Value * 100)}";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRouting)
            {
                LoadDevices();
            }
            else
            {
                MessageBox.Show("Please stop routing before refreshing devices.");
            }
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRouting)
            {
                var sourceDevice = SourceComboBox.SelectedItem as MMDevice;
                if (sourceDevice == null) return;

                var destDevices = new List<MMDevice>();
                foreach (StackPanel container in DestinationsPanel.Children)
                {
                    var cb = container.Children[0] as CheckBox;
                    if (cb != null && cb.IsChecked == true)
                    {
                        destDevices.Add(cb.Tag as MMDevice);
                    }
                }

                if (destDevices.Count == 0)
                {
                    MessageBox.Show("Please select at least one destination device.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    _audioEngine.StartRouting(sourceDevice, destDevices);
                    _isRouting = true;
                    StartStopButton.Content = "Stop Routing";
                    StartStopButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error starting routing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _audioEngine.StopRouting();
                _isRouting = false;
                StartStopButton.Content = "Start Routing";
                StartStopButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 134, 54)); // GitHub Green
            }
        }

        private async void InstallDriverButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.Content = "Downloading...";
            btn.IsEnabled = false;

            try
            {
                string zipPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VBCABLE_Driver.zip");
                string extractPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VBCABLE_Driver");

                using (var client = new System.Net.Http.HttpClient())
                {
                    var bytes = await client.GetByteArrayAsync("https://download.vb-audio.com/Download_CABLE/VBCABLE_Driver_Pack43.zip");
                    System.IO.File.WriteAllBytes(zipPath, bytes);
                }

                if (System.IO.Directory.Exists(extractPath))
                    System.IO.Directory.Delete(extractPath, true);

                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);

                string exePath = System.IO.Path.Combine(extractPath, "VBCABLE_Setup_x64.exe");

                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                var proc = System.Diagnostics.Process.Start(processInfo);
                await System.Threading.Tasks.Task.Run(() => proc.WaitForExit());

                MessageBox.Show("Driver installation finished! Please restart the app for the new Virtual Device to appear.", "Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Failed to install driver: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btn.Content = "Install Virtual Cable (Requires Admin)";
                btn.IsEnabled = true;
            }
        }
    }
}
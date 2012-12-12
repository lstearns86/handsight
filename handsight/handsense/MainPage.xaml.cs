using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using handsight.Resources;
using System.Windows.Shapes;
using System.Windows.Media;

using Windows.Networking.Proximity;
using Windows.System;
using Windows.Networking;
using Windows.Networking.Sockets;
using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Threading;

namespace handsight
{
    public partial class MainPage : PhoneApplicationPage
    {
        private Handsight device = null;
        private bool connected = false;

        private List<Line> lines = new List<Line>();
        //private Polyline line;
        private int[] sensors;

        private enum Mode { Edges, Black, Grayscale, Navigation, Typing, Massage }
        private Mode mode = Mode.Edges;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            sensors = new int[6];
            for (int i = 0; i < 6; i++)
            {
                Line line = new Line();
                line.X1 = i * (Graph.Width - 100) / 6 + 75;
                line.X2 = line.X1;
                line.Y1 = 0;
                line.Y2 = Graph.Height;
                line.Stroke = new SolidColorBrush(Colors.Green);
                line.Fill = new SolidColorBrush(Colors.Green);
                line.StrokeThickness = 30;
                lines.Add(line);
                Graph.Children.Add(line);
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (device == null || connected == false)
            {
                StatusLabel.Text = "Connecting...";
                bool success = await SetupDeviceConn();
                if (success)
                {
                    device.Update += DeviceUpdate;
                    StatusLabel.Text = "Connected";
                    ConnectButton.Content = "Disconnect";
                    connected = true;
                }
                else
                {
                    StatusLabel.Text = "Not Connected";
                }
            }
            else
            {
                StatusLabel.Text = "Disconnecting...";
                device.Update -= DeviceUpdate;
                device.Stop(Dispatcher);
                StatusLabel.Text = "Disconnected";
                ConnectButton.Content = "Connect";
                connected = false;
            }
        }

        private void DeviceUpdate(int mode, int[] values, string text)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Mode newMode = (Mode)Enum.ToObject(typeof(Mode), mode);
                if (newMode != this.mode)
                {
                    this.mode = newMode;
                    switch (this.mode)
                    {
                        case Mode.Edges: EdgesButton.IsChecked = true; break;
                        case Mode.Black: BlackButton.IsChecked = true; break;
                        case Mode.Grayscale: GrayscaleButton.IsChecked = true; break;
                        case Mode.Navigation: NavigationButton.IsChecked = true; break;
                        case Mode.Typing: TypingButton.IsChecked = true; break;
                        case Mode.Massage: MassageButton.IsChecked = true; break;
                        default: break;
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    sensors[i] = values[i];
                    lines[i].Y1 = (1 - values[i] / 1024.0) * Graph.Height;
                }
                
                if (text.Length > 0)
                {
                    TextDisplay.Text += text;
                    if (TextDisplay.Text.Length > 50)
                        TextDisplay.Text = TextDisplay.Text.Substring(TextDisplay.Text.Length - 50, 50);
                }
            });

        }

        private async Task<bool> SetupDeviceConn()
        {
            //Connect to your paired NXTCar using BT + StreamSocket (over RFCOMM)
            PeerFinder.AlternateIdentities["Bluetooth:PAIRED"] = "";

            var devices = await PeerFinder.FindAllPeersAsync();
            if (devices.Count == 0)
            {
                MessageBox.Show("No bluetooth devices are paired, please pair your device");
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                return false;
            }

            PeerInformation peerInfo = devices.FirstOrDefault(c => c.DisplayName.Contains("handsense"));
            if (peerInfo == null)
            {
                MessageBox.Show("No paired device was found, please pair your handsense device");
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                return false;
            }

            StreamSocket s = new StreamSocket();
            //This would ask winsock to do an SPP lookup for us; i.e. to resolve the port the 
            //device is listening on
            try
            {
                await s.ConnectAsync(peerInfo.HostName, "{00001101-0000-1000-8000-00805F9B34FB}");
            }
            catch { }

            device = new Handsight(s);
            return device.IsConnected;
        }

        private void EdgesButton_Checked(object sender, RoutedEventArgs e)
        {
            if (mode != Mode.Edges)
            {
                mode = Mode.Edges;
                if (device != null && device.IsConnected) device.SetMode((int)mode);
            }
        }


        private void BlackButton_Checked(object sender, RoutedEventArgs e)
        {
            if (mode != Mode.Black)
            {
                mode = Mode.Black;
                if(device != null && device.IsConnected) device.SetMode((int)mode);
            }
        }

        private void GrayscaleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (mode != Mode.Grayscale)
            {
                mode = Mode.Grayscale;
                if (device != null && device.IsConnected) device.SetMode((int)mode);
            }
        }

        private void NavigationButton_Checked(object sender, RoutedEventArgs e)
        {
            if (mode != Mode.Navigation)
            {
                mode = Mode.Navigation;
                if (device != null && device.IsConnected) device.SetMode((int)mode);
            }
        }

        private void TypingButton_Checked(object sender, RoutedEventArgs e)
        {
            if (mode != Mode.Typing)
            {
                mode = Mode.Typing;
                if (device != null && device.IsConnected) device.SetMode((int)mode);
            }
        }

        private void MassageButton_Checked(object sender, RoutedEventArgs e)
        {
            if (mode != Mode.Massage)
            {
                mode = Mode.Massage;
                if (device != null && device.IsConnected) device.SetMode((int)mode);
            }
        }
    }
}
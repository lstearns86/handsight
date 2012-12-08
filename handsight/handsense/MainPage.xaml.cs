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

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            sensors = new int[5];
            for (int i = 0; i < 5; i++)
            {
                Line line = new Line();
                line.X1 = i * (Graph.Width - 100) / 5 + 75;
                line.X2 = line.X1;
                line.Y1 = 0;
                line.Y2 = Graph.Height;
                line.Stroke = new SolidColorBrush(Colors.Green);
                line.Fill = new SolidColorBrush(Colors.Green);
                line.StrokeThickness = 30;
                lines.Add(line);
                Graph.Children.Add(line);
            }

            //for (int i = 0; i < Graph.Width / 3; i++)
            //{
            //    Line line = new Line();
            //    line.X1 = 3 * i;
            //    line.X2 = 3 * (i + 1);
            //    line.Y1 = Graph.Height;
            //    line.Y2 = Graph.Height;
            //    line.Stroke = new SolidColorBrush(Colors.Blue);
            //    line.Fill = new SolidColorBrush(Colors.Blue);
            //    lines.Add(line);
            //    Graph.Children.Add(line);
            //}

            //line = new Polyline();
            //line.Stroke = new SolidColorBrush(Colors.Blue);
            //PointCollection points = new PointCollection();
            //for (int i = 0; i < Graph.Width / 2; i++)
            //    points.Add(new System.Windows.Point(i * 2, Graph.Height));
            //line.Points = points;
            //Graph.Children.Add(line);

            // TODO: Delete
            //Task.Factory.StartNew(() =>
            //{
            //    Random rand = new Random();
            //    Thread.Sleep(2000);
            //    int last = 0;
            //    while (true)
            //    {
            //        int value = last + rand.Next(20) - 10;
            //        if (value < 0) value = 0;
            //        if (value > 1023) value = 1023;
            //        DeviceUpdate(value);
            //        last = value;
            //        Thread.Sleep(10);
            //    }
            //});

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
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

        private void DeviceUpdate(int[] values)
        {
            Dispatcher.BeginInvoke(() =>
            {
                //ValueLabel.Text = "Value: " + value;

                lines[0].Y1 = (1 - values[0] / 1024.0) * Graph.Height;
                //lines[1].Y1 = (1 - values[1] / 1024.0) * Graph.Height;
                //lines[2].Y1 = (1 - values[2] / 1024.0) * Graph.Height;
                //lines[3].Y1 = (1 - values[3] / 1024.0) * Graph.Height;
                //lines[4].Y1 = (1 - values[4] / 1024.0) * Graph.Height;

                //for (int i = 0; i < lines.Count - 1; i++)
                //{
                //    lines[i].Y1 = lines[i + 1].Y1;
                //    lines[i].Y2 = lines[i + 1].Y2;
                //}
                //lines[lines.Count - 1].Y1 = lines[lines.Count - 1].Y2;
                //lines[lines.Count - 1].Y2 = (1 - value / 1024.0) * Graph.Height;
                
                //PointCollection points = new PointCollection();
                //for (int i = 0; i < line.Points.Count - 1; i++)
                //{
                //    System.Windows.Point p = line.Points[i];
                //    p.Y = line.Points[i + 1].Y;
                //    points.Add(p);
                //}
                //System.Windows.Point newPoint = line.Points[line.Points.Count - 1];
                //newPoint.Y = (1 - value / 1024.0) * Graph.Height;
                //points.Add(newPoint);
                //line.Points = points;
            });

        }

        private void VibrateButton_Click(object sender, RoutedEventArgs e)
        {
            if (device != null && device.IsConnected)
            {
                device.ToggleVibrate();
            }
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
            //await s.ConnectAsync(peerInfo.HostName, "1");

            //This would ask winsock to do an SPP lookup for us; i.e. to resolve the port the 
            //device is listening on
            //await s.ConnectAsync(peerInfo.HostName, "{00001101-0000-1000-8000-00805F9B34FB}");
            try
            {
                await s.ConnectAsync(peerInfo.HostName, "{00001101-0000-1000-8000-00805F9B34FB}");
            }
            catch { }

            device = new Handsight(s);
            return device.IsConnected;
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}
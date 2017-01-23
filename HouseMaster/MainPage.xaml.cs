using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HouseMaster
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        HomeMasterClient _clientSocket;

        // degree symbol = alt+0176 
        const string celciusSymbol = "°C";
        const string fahrenheitSymbol = "°F";
        bool isCelcius = true;  //default

        bool isConnected = false;

        public MainPage()
        {
            this.InitializeComponent();
            txtPort.Text = "9000";
            txtIp.Text = "192.168.0.24";
        }

        private void btnConnect_Click(object o, RoutedEventArgs e)
        {
            if (isConnected)
            {
                // true: disconnect client
                _clientSocket.Close();
                _clientSocket.OnDataReceived -= Socket_OnDataRecived;
                _clientSocket = null;

                ClearDataValues();

                txtIp.IsReadOnly = false;
                txtPort.IsReadOnly = false;
                txtIp.Background = new SolidColorBrush(Windows.UI.Colors.White);
                txtPort.Background = new SolidColorBrush(Windows.UI.Colors.White);

                btnConnect.Content = TextStrings.Connect;
                isConnected = false;

                txbStatus.Text = isConnected == true ? TextStrings.Connected : TextStrings.Stopped;
            }
            else
            {
                // false: try to connect client
                if (_clientSocket != null)
                {
                    _clientSocket.Close();
                    _clientSocket.OnDataReceived -= Socket_OnDataRecived;
                    _clientSocket = null;
                }

                _clientSocket = new HomeMasterClient(txtIp.Text, Convert.ToInt32(txtPort.Text));
                _clientSocket.Connect();
                _clientSocket.OnDataReceived += Socket_OnDataRecived;

                isConnected = true;
                txbStatus.Text = isConnected == true ? TextStrings.Connected : TextStrings.Stopped;

                txtIp.IsReadOnly = true;
                txtPort.IsReadOnly = true;
                txtIp.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
                txtPort.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);

                btnConnect.Content = TextStrings.Disconnect;
            }
        }

        private void Socket_OnDataRecived(string data)
        {
            // parse out new data and display
            if (!string.IsNullOrEmpty(data))
            {
                DisplayData(data);
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            _clientSocket.Send(txtMessage.Text);
        }

        private void DisplayData(string data)
        {
            string sensorId = data.Substring(0, 1);
            string reading = data.Substring(1);
            int value;
            Int32.TryParse(reading, out value);

            switch (sensorId)
            {
                case "g":
                    // garage door
                    txbGarDoorValue.Text = DoorStateString(value);
                    break;

                case "k":
                    // kitchen door
                    txbKitDoorValue.Text = DoorStateString(value);
                    break;

                case "l":
                    // garage light
                    txbGarLightsValue.Text = LightStateString(value);
                    break;

                case "a":
                    // bay A
                    txbGarBayAValue.Text = BayStateString(value);
                    break;

                case "i":
                    // bay B
                    txbGarTempValue.Text = string.Format("{0}{1}", value.ToString(), isCelcius ? celciusSymbol : fahrenheitSymbol);
                    break;

                case "c":
                    // front temp
                    txbFrontTempValue.Text = string.Format("{0}{1}", value.ToString(), isCelcius ? celciusSymbol : fahrenheitSymbol);
                    break;

                case "h":
                    // front humidity
                    txbFronthumidValue.Text = string.Format("{0}{1}", value.ToString(), "%");
                    break;
                    
                case "x":
                    // front HeatIndex
                    txbFrontHIValue.Text = string.Format("{0}{1}", value.ToString(), isCelcius ? celciusSymbol : fahrenheitSymbol);
                    break;

                default:
                    break;
            }

            #region sample data

            /*
            GarageDoorState: Closed = 0, Open = 1, Closing = 2, , Opening = 3;

            example data:
            g0 - Garage Closed
            g1 - Garage Open
            g2 - Garage Closing...
            g3 - Garage Opening...

            k0 - Kitchen Closed
            K1 - Kitchen Open

            l0 - Light Off
            l1 - Light On

            a0 - Garage Bay 1 Occupied
            a1 - Garage Bay 1 Vacant

            b0 - Garage Bay 2 Occupied
            b1 - Garage Bay 2 Vacant
            

            tc16.90 - Temp C
            th59.70 - Humidity
            ta16.20 - Heat Index C
            ti16.95 - Internal Temp C (from breakout board)

            gu1 - Garage Upper
            gl0 - Garage Lower
            kd1 - Kitchen Door
            la145 - Light Reading
            sa117 - Sonar Reading A
            sb117 - Sonar Reading B
            tc16.90 - Temp C
            th59.70 - Humidity
            ta16.20 - Heat Index C
            ti16.95 - Internal Temp C (from breakout board)

            */

            #endregion
        }

        private string DoorStateString(int data)
        {
            string statusString = string.Empty;
            switch (data)
            {
                case 0:
                    statusString = "Closed";
                    break;

                case 1:
                    statusString = "Open";
                    break;

                case 2:
                    statusString = "Closing...";
                    break;

                case 3:
                    statusString = "Opening...";
                    break;

                default:
                    break;
            }

            return statusString;
        }

        private string LightStateString(int data)
        {
            string statusString = string.Empty;
            switch (data)
            {
                case 0:
                    statusString = "Off";
                    break;

                case 1:
                    statusString = "On";
                    break;

                default:
                    break;
            }

            return statusString;
        }

        private string BayStateString(int data)
        {
            string statusString = string.Empty;
            switch (data)
            {
                case 0:
                    statusString = "Occupied";
                    break;

                case 1:
                    statusString = "Vacant"; 
                    break;

                default:
                    break;
            }

            return statusString;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // clear out the UI values
            ClearDataValues();

            // send a refresh command
            _clientSocket.Send("r0");
        }

        private void ClearDataValues()
        {
            txbKitDoorValue.Text = ".........";
            txbGarDoorValue.Text = ".........";
            txbGarLightsValue.Text = ".........";
            txbGarBayAValue.Text = ".........";
            txbGarBayBValue.Text = ".........";
            txbGarTempValue.Text = ".........";
            txbFrontTempValue.Text = ".........";
            txbFronthumidValue.Text = ".........";
            txbFrontHIValue.Text = ".........";
        }

        public class TextStrings
        {
            public static string Connected
            {
                get { return "Connected."; }
            }

            public static string Running
            {
                get { return "Running..."; }
            }

            public static string Stopped
            {
                get { return "Stopped."; }
            }

            public static string Connect
            {
                get { return "Connect"; }
            }

            public static string Disconnect
            {
                get { return "Disconnect"; }
            }
        }
    }
}

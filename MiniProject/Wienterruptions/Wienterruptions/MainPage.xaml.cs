using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akavache;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace Wienterruptions
{
    public partial class MainPage : ContentPage
    {
        private static DeviceClient deviceClient;
        private static string iotHubUrl = "AseClientHub.azure-devices.net";
        private static string deviceKey = "6GK+jes33g7i08OjNps0e1HpajFrl2You7U35zxpiaU=";
        private static string deviceId = "XamarinClient";

        private static string connectionString =
            $"HostName={iotHubUrl};DeviceId={deviceId};SharedAccessKey={deviceKey}";

        private bool isMessaging = false;
        private bool isListening = false;

        public MainPage()
        {
            InitializeComponent();

            BlobCache.ApplicationName = "XamarinClient";

            //deviceClient = DeviceClient.Create(iotHubUrl, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
        }

        private void StartButton_OnClicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Sending messages";
            SendDeviceToCloudMessageAsync();
        }

        private void StopButton_OnClicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Not sending messages";
            isMessaging = false;
        }

        private void StartListening_OnClicked(object sender, EventArgs e)
        {
            LogLabel.Text = "Listening for messages...";
            ReceiveCloudToDeviceMessagesAsync();
        }

        private void StopListening_OnClicked(object sender, EventArgs e)
        {
            isListening = false;
        }

        private async void ReceiveCloudToDeviceMessagesAsync()
        {
            isListening = true;
            while (isListening)
            {
                var message = await deviceClient.ReceiveAsync();
                if (message == null)
                {
                    continue;
                }

                var bytes = message.GetBytes();
                LogLabel.Text += "\n" + Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                await deviceClient.CompleteAsync(message);
            }
        }

        private async void SendDeviceToCloudMessageAsync()
        {
            double minTemperature = 20;
            double minHumidity = 60;
            int messageId = 1;
            Random rand = new Random();

            isMessaging = true;
            while (isMessaging)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                var telemetryDataPoint = new
                {
                    messageId = messageId++,
                    deviceId = deviceId,
                    temperature = currentTemperature,
                    humidity = currentHumidity
                };

                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.UTF8.GetBytes(messageString));
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                await deviceClient.SendEventAsync(message);

                await Task.Delay(1000);
            }
        }
    }
}
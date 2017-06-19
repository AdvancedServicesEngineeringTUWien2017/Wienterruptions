using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public MainPage()
        {
            InitializeComponent();

            //deviceClient = DeviceClient.Create(iotHubUrl, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
        }

        private void Button_OnClicked(object sender, EventArgs e)
        {
            SendDeviceToCloudMessageAsync();
        }

        private static async void SendDeviceToCloudMessageAsync()
        {
            double minTemperature = 20;
            double minHumidity = 60;
            int messageId = 1;
            Random rand = new Random();

            while (true)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minTemperature + rand.NextDouble() * 20;

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plugin.Geolocator;
using Xamarin.Forms;

namespace Wienterruptions
{
    public partial class MainPage : ContentPage
    {
        private static DeviceClient deviceClient;
        private static string iotHubUrl = "AseClientHub.azure-devices.net";
        private static string deviceGroupKey = "6GK+jes33g7i08OjNps0e1HpajFrl2You7U35zxpiaU=";
        private static string deviceGroupId = "XamarinClient";

        private bool isListening = false;

        //TODO remove after integrating shared properties
        private string temporaryDeviceId = null;

        private string temporaryDeviceKey = null;

        public MainPage()
        {
            InitializeComponent();
        }

        private void RegisterButton_OnClicked(object sender, EventArgs e)
        {
            RegisterDeviceId();
        }

        private void ConnectButton_OnClicked(object sender, EventArgs e)
        {
            ConnectToIoTHub();
        }

        private void StartListening_OnClicked(object sender, EventArgs e)
        {
            AddLogEntry("Started listening for messages...");
            ReceiveCloudToDeviceMessagesAsync();
        }

        private void StopListening_OnClicked(object sender, EventArgs e)
        {
            isListening = false;
            AddLogEntry("Stopped listening for messages.");
        }

        private void SendSettings_OnClicked(object sender, EventArgs e)
        {
            SendSettingsToCloudAsync();
        }

        private void UpdateLocation_OnClicked(object sender, EventArgs e)
        {
            UpdateLocationAsync();
        }

        private async void UpdateLocationAsync()
        {
            try
            {
                AddLogEntry("Updating location...");
                var locator = CrossGeolocator.Current;
                locator.DesiredAccuracy = 20; //TODO make more accurate?

                var position = await locator.GetPositionAsync(10000);

                LongitudeEntry.Text = position.Longitude.ToString("##.######");
                LatitudeEntry.Text = position.Latitude.ToString("##.######");
                AddLogEntry("...done");
            }
            catch (Exception e)
            {
                AddLogEntry("...failed");
            }
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
                JObject metadata = (JObject)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
                AddInterruptionEntry(metadata);
                await deviceClient.CompleteAsync(message);
            }
        }

        private async void SendSettingsToCloudAsync()
        {
            AddLogEntry("Sending device settings to cloud...");
            var messageId = Guid.NewGuid();
            var deviceId = GetDeviceId();

            JObject messageObject = new JObject();
            messageObject["messageId"] = messageId;
            messageObject["deviceId"] = deviceId;
            messageObject["lines"] = JToken.Parse(LinesEntry.Text);

            JObject location = new JObject();
            location["type"] = "Point";
            double[] coordinates = {Double.Parse(LongitudeEntry.Text), Double.Parse(LatitudeEntry.Text)};
            location["coordinates"] = JToken.FromObject(coordinates);

            messageObject["location"] = location;

            var messageString = JsonConvert.SerializeObject(messageObject);
            var message = new Message(Encoding.UTF8.GetBytes(messageString));
            message.Properties.Add("type", "userdata");
            await deviceClient.SendEventAsync(message);

            AddLogEntry("...sent");
        }

        private string GetDeviceId()
        {
            //TODO get id from storage
            /*if (temporaryDeviceId == null)
            {
                temporaryDeviceId = Guid.NewGuid().ToString();
            }
            return temporaryDeviceId;*/
            return "db26b90c-0b71-4e88-836b-7cae3d772e8c";
        }

        private void ConnectToIoTHub()
        {
            var deviceId = GetDeviceId();
            AddLogEntry("Connecting with device id '" + deviceId + "'...");
            string connectionString = $"HostName={iotHubUrl};DeviceId={deviceId};SharedAccessKey={temporaryDeviceKey}";
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
            AddLogEntry("...connected");
        }

        private async void RegisterDeviceId()
        {
            var deviceId = GetDeviceId();
            AddLogEntry("Registering device id '" + deviceId + "'...");
            string connectionString = $"HostName={iotHubUrl};DeviceId={deviceGroupId};SharedAccessKey={deviceGroupKey}";
            DeviceClient registrationDeviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
            var handshakeId = Guid.NewGuid().ToString();

            var messageData = new
            {
                deviceId = deviceId,
                handshakeId = handshakeId
            };
            var messageJson = JsonConvert.SerializeObject(messageData);

            var message = new Message(Encoding.UTF8.GetBytes(messageJson));
            message.Properties.Add("type", "register");

            await registrationDeviceClient.SendEventAsync(message);

            while (temporaryDeviceKey == null)
            {
                var response = await registrationDeviceClient.ReceiveAsync();
                if (response == null)
                {
                    continue;
                }

                var bytes = response.GetBytes();
                var responseString = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                dynamic responseObject = JsonConvert.DeserializeObject(responseString);
                var responseHandshakeId = responseObject.handshakeId.ToString();
                if (responseObject.handshakeId.ToString() == handshakeId)
                {
                    temporaryDeviceKey = responseObject.primaryKey;
                }
                await registrationDeviceClient.CompleteAsync(response);
            }

            await registrationDeviceClient.CloseAsync();
            AddLogEntry("...registered");
        }

        private void AddLogEntry(string message)
        {
            Label label = new Label();
            label.Text = message;
            LogList.Children.Insert(0, label);
        }

        private void AddInterruptionEntry(JObject metadata)
        {
            Label label = new Label();

            StringBuilder messageBuilder = new StringBuilder("Interruption:\nLine: ");
            messageBuilder.Append(metadata["linename"]);
            messageBuilder.Append("\nTowards: ");
            messageBuilder.Append(metadata["towards"]);
            messageBuilder.Append("\nStation: ");
            messageBuilder.Append(metadata["station"]);
            var distance = metadata["distance"];
            if (distance != null)
            {
                messageBuilder.Append("\nDistance: ");
                messageBuilder.Append(metadata["distance"]);
                double distanceValue = Double.Parse(distance.ToString());
                if (distanceValue < 500)
                {
                    label.TextColor = Color.Crimson;
                }
            }
            
            label.Text = messageBuilder.ToString();
            LogList.Children.Insert(0, label);
        }
    }
}
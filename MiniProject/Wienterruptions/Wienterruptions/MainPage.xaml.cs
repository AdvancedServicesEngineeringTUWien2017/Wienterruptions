using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
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

        private void StartButton_OnClicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Sending messages";
            SendDeviceToCloudMessageAsync();
        }

        private void StopButton_OnClicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Not sending messages";
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
            var messageId = Guid.NewGuid();
            var deviceId = GetDeviceId();

            var dataPoint1 = new
            {
                messageId = messageId,
                deviceId = deviceId,
                line = "49"
            };

            messageId = Guid.NewGuid();
            var dataPoint2 = new
            {
                messageId = messageId,
                deviceId = deviceId,
                line = "N49"
            };

            var messageString = JsonConvert.SerializeObject(dataPoint1);
            var message = new Message(Encoding.UTF8.GetBytes(messageString));
            message.Properties.Add("type", "userdata");
            await deviceClient.SendEventAsync(message);

            messageString = JsonConvert.SerializeObject(dataPoint2);
            message = new Message(Encoding.UTF8.GetBytes(messageString));
            message.Properties.Add("type", "userdata");
            await deviceClient.SendEventAsync(message);
        }

        private string GetDeviceId()
        {
            //TODO get id from storage
            if (temporaryDeviceId == null)
            {
                temporaryDeviceId = Guid.NewGuid().ToString();
            }
            return temporaryDeviceId;
        }

        private void ConnectToIoTHub()
        {
            var deviceId = GetDeviceId();
            string connectionString = $"HostName={iotHubUrl};DeviceId={deviceId};SharedAccessKey={temporaryDeviceKey}";
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
        }

        private async void RegisterDeviceId()
        {
            string connectionString = $"HostName={iotHubUrl};DeviceId={deviceGroupId};SharedAccessKey={deviceGroupKey}";
            DeviceClient registrationDeviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Http1);
            var deviceId = GetDeviceId();
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
            StatusLabel.Text = "Finished registering";
        }
    }
}
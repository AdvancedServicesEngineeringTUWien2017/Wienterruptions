using System.Text;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

private static string connectionString = "HostName=AseClientHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=vCkk8SgqouOM2JytAgpxRaTRZPFEsrDvl/7Frr2ctl0=";

public static void Run(string mySbMsg, TraceWriter log)
{
    log.Info($"C# ServiceBus topic trigger function processed message: {mySbMsg}");
    log.Info($"####################################");

    dynamic status = JsonConvert.DeserializeObject(mySbMsg);
    log.Info($"Deserialized Message: {status.ToString()}");
    
    var deviceId = status.deviceid;
    log.Info("DeviceId: " + deviceId);

    // create IoT Hub connection.
    ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

    // Composing the command message.
    var commandMessage = new Message(Encoding.ASCII.GetBytes(mySbMsg));
    // send command to specified device.
    log.Info($"Send interruption to Device {deviceId}");
    serviceClient.SendAsync(deviceId.ToString(), commandMessage);
}
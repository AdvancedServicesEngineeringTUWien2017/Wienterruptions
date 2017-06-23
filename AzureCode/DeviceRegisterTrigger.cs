using System.Text;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

private static RegistryManager registryManager;
private static string connectionString = "HostName=AseClientHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=vCkk8SgqouOM2JytAgpxRaTRZPFEsrDvl/7Frr2ctl0=";
private static TraceWriter logger;
private static string shakeId;

public static void Run(string myQueueItem, TraceWriter log)
{
    logger = log;
    log.Info("Register Message was Triggered!");
    log.Info($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

    dynamic status = JsonConvert.DeserializeObject(myQueueItem);
    var deviceId = status.deviceId;
    shakeId = status.handshakeId;
    log.Info("DeviceId: " + deviceId);

    registryManager = RegistryManager.CreateFromConnectionString(connectionString);
    AddDeviceAsync(deviceId.ToString()).Wait();
}


private static async Task AddDeviceAsync(string deviceId)
{
    logger.Info($"Registering new client with ID: {deviceId}");

    Device device;
    try
    {
        device = await registryManager.AddDeviceAsync(new Device(deviceId));
    }
    catch (Exception)
    {
        device = await registryManager.GetDeviceAsync(deviceId);
    }

    var key = device.Authentication.SymmetricKey.PrimaryKey;

    logger.Info($"Generated device key: {key}");

    logger.Info("Sending key to device!");
    SendKeyBack(key);
    
}

private static ServiceClient serviceClient;
private static string connectionStringDevice = "HostName=AseClientHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=vCkk8SgqouOM2JytAgpxRaTRZPFEsrDvl/7Frr2ctl0=";

private static void SendKeyBack(string key)
{
     logger.Info("Send Cloud-to-Device message\n");
    serviceClient = ServiceClient.CreateFromConnectionString(connectionStringDevice);
    SendCloudToDeviceMessageAsync(key).Wait();
}

private async static Task SendCloudToDeviceMessageAsync(string key)
{
    logger.Info($"HandShake Id: {shakeId}");
    var message = new
    {
        handshakeId = shakeId,
        primaryKey = key
    };

    var messageJson = JsonConvert.SerializeObject(message);

    var commandMessage = new Message(Encoding.UTF8.GetBytes(messageJson));
    await serviceClient.SendAsync("XamarinClient", commandMessage);
     logger.Info("Message is sent");
}
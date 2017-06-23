# Goal
The goal of this project was to implement an app that will notify the user about interruptions of the Wiener Linien public transport system. 
The user should have the possibility to register for some lines he wishes to observe, as well as register with his GPS coordinates and get notifications about all 
interruption which occur at those lines or are near him. 

# Implementation
We used Xamarin as framework for the App. The App is sending User Settings via EventHubs to an Azure cloud where the Sensor Data from Wiener Linien is processed.
The result is then sent back to the clients via IoT Hubs. The client will display the results. 

# Note
All connection strings and blade names need to be changed in the source code!!

# How to set up the Azure Cloud

## Add needed Blades

### Event Hub
Add an "Event Hub" blade to your Azure account.

### Service Bus
Add these 3 topics: 

	interruptionnotificationtopic
	registertopic
	userdatatopic


### IoT Hub
Add an "IoT Hub" blade to your Azure account. Add two endpoints. RegisterTopicEndpoint and UserDataEndpoint. 
Configure 2 Routes:

Name: UserDataRoute, From: DeviceMessages, Where: type="userdata", Into: UserDataEndpoint 
Name:RegisterTopicRoute, From: DeviceMessages, Where: type="register", Into: RegisterTopicEndpoint true

### Storage
Add a Storage account
Whenever a Blade need a storage account provide it with the name of this one. 
Add a new Blob Container for the user reference data. Named: UserReferenceData

### Function App
Add a storage app
Add 3 functions to the app:

#### DeviceRegisterTrigger
Message type: Service Bus Topic
Topic name: registertopic
Service Bus connection: add the connection to your service bus

Add the following code to the function:
```
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
```

#### UserSettingsTrigger
Message type: Service Bus Topic
Topic name: userdatatopic

Add the following code to the function:
```
using System;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

private static string storageConectionString = "DefaultEndpointsProtocol=https;AccountName=asewienerlinienstorage;AccountKey=LbDH/+nnRAGq9QrRYh3aAv4DokCP93zIpm5zmn/6W3wRwgszGl6yDIi1CojN10dM5nx3fUGIZkHzaG70ngaANQ==;EndpointSuffix=core.windows.net";

public static void Run(string mySbMsg, TraceWriter log)
{
    log.Info($"C# ServiceBus topic trigger function processed message: {mySbMsg}");
    var storageAccount = CloudStorageAccount.Parse(storageConectionString);
    var blobClient = storageAccount.CreateCloudBlobClient();
    var container = blobClient.GetContainerReference("userreferencedata");
    var blob = container.GetBlockBlobReference("userSettings.json");

    dynamic message = JsonConvert.DeserializeObject(mySbMsg);
    log.Info($"Message: {message}");
    var deviceId = message.deviceId.ToString();
    var lines = message.lines;

    StringBuilder uploadBuilder = new StringBuilder();

    if (blob.Exists())
    {
        var stream = blob.OpenRead();
        StreamReader reader = new StreamReader(stream);
        string line;
       
        while (!String.IsNullOrEmpty(line = reader.ReadLine()))
        {
            var json = JObject.Parse(line);
            if (json.GetValue("deviceId").ToString() != deviceId)
            {
                uploadBuilder.AppendLine(line);
            }
        }
    }

    foreach(var line in lines)
    {
        JObject entry = new JObject();
        entry["deviceId"] = deviceId;
        entry["line"] = line;
        entry["location"] = message.location;

        uploadBuilder.AppendLine(JsonConvert.SerializeObject(entry));
    }

     string upload = uploadBuilder.ToString();
     blob.UploadText(upload);
}
```

#### NotifyUserTrigger
Message type: Service Bus Topic
Topic name: interruptionnotificationtopic
Service Bus connection: use the same service bus connection as for the previous trigger

Add the following code to your function
```
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
```

### Stream Analytics
Create a new Stream Analytics Job

Add the following inputs:

* SensorInput from Event Hub
* UserReferenceInput from Blob storage linked to userreferencdata

Add the following outputs:

* IoTOutput with Service bus Topic as sink

Set the following query:
```
SELECT
    line.ArrayValue.name as linename,
    line.ArrayValue.towards,
    L1.locationStop.properties.title as station,
    L2.deviceId,
    ST_DISTANCE(L1.locationStop.geometry, L2.location) as distance
INTO
    IoTOutput
FROM SensorInput L1
    CROSS APPLY GetArrayElements(L1.lines) AS line
    Join  UserReferenceInput L2
    on line.ArrayValue.name = L2.line
WHERE
    line.ArrayValue.trafficjam = 1
```
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
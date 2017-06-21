using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlobStorageManipulator
{
    class Program
    {
        static void Main(string[] args)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=asewienerlinienstorage;AccountKey=LbDH/+nnRAGq9QrRYh3aAv4DokCP93zIpm5zmn/6W3wRwgszGl6yDIi1CojN10dM5nx3fUGIZkHzaG70ngaANQ==;EndpointSuffix=core.windows.net");
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference("userreferencedata");
            //var blob = container.GetBlockBlobReference("userSettings.json");

            Random random = new Random();
            string[] deviceIds = { "deviceId1", "deviceId2", "deviceId3", "deviceId4", "deviceId5" };
            while (true)
            {
                Console.WriteLine("Press RETURN to generate a new entry...");
                Console.ReadLine();

                string deviceId = deviceIds[random.Next(deviceIds.Length)];

                StringBuilder uploadBuilder = new StringBuilder();

                var blob = container.GetBlockBlobReference("userSettings.json");
                if (blob.Exists())
                {
                    var stream = blob.OpenRead();
                    //byte[] buffer = new byte[2048];
                    //MemoryStream stream = new MemoryStream(buffer);
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

                //TODO parse user setting and iterate over lines
                for (int i = 0; i < random.Next(1, 4); i++)
                {
                    JObject entry = new JObject();
                    entry["deviceId"] = deviceId;
                    entry["line"] = random.Next(1, 50);

                    uploadBuilder.AppendLine(JsonConvert.SerializeObject(entry));
                }

                string upload = uploadBuilder.ToString();
                blob.UploadText(upload);
            }
        }
    }
}

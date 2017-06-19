using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;

namespace EventHubReceiver
{
    class Program
    {
        private const string EhConnectionString = "Endpoint=sb://sensorinputevents.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=qZUuYmnG/P7WWjFcQ1V1voFWzL5l8HTsOAj54ozKDGI=";
        private const string EhEntityPath = "sensoroutput";
        private const string StorageContainerName = "asewinerliniencontainer";
        private const string StorageAccountName = "asewienerlinienstorage";
        private const string StorageAccountKey = "LbDH/+nnRAGq9QrRYh3aAv4DokCP93zIpm5zmn/6W3wRwgszGl6yDIi1CojN10dM5nx3fUGIZkHzaG70ngaANQ==";

        private static readonly string StorageConnectionString =
            string.Format(
                "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                StorageAccountName,
                StorageAccountKey);

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Registering EventProcessor...");

            var eventProcessorHost = new EventProcessorHost(
                EhEntityPath,
                PartitionReceiver.DefaultConsumerGroupName,
                EhConnectionString,
                StorageConnectionString,
                StorageContainerName);

            await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>();

            Console.WriteLine("Receiving. Press ENTER to stop worker...");
            Console.ReadLine();

            await eventProcessorHost.UnregisterEventProcessorAsync();
        }
    }
}

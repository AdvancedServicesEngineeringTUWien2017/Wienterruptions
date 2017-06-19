using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace ReadDeviceToCloudMessages
{
    class Program
    {
        private static string connectionString =
                "HostName=AseClientHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=vCkk8SgqouOM2JytAgpxRaTRZPFEsrDvl/7Frr2ctl0="
            ;

        private static string iotHubD2cEndpoint = "messages/events";
        private static EventHubClient eventHubClient;

        static void Main(string[] args)
        {
            Console.WriteLine("Receive messages, Ctrl-C to exit.\n");
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            System.Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
                Console.WriteLine("Exiting...");
            };

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cancellationTokenSource.Token));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken cancellationToken)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) {break;}
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) {continue;}

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                Console.WriteLine("Message received. Partition: '{0}', Data: '{1}'", partition, data);
            }
        }
    }
}

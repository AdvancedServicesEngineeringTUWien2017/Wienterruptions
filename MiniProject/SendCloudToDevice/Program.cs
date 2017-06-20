using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace SendCloudToDevice
{
    class Program
    {
        private static ServiceClient serviceClient;
        private static string connectionString = "HostName=AseClientHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=vCkk8SgqouOM2JytAgpxRaTRZPFEsrDvl/7Frr2ctl0=";

        static void Main(string[] args)
        {
            Console.WriteLine("Send Cloud-to-Device message\n");
            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            while (true)
            {
                Console.WriteLine("Press any key to send a message...");
                Console.ReadLine();
                SendCloudToDeviceMessageAsync().Wait();
            }
        }

        private async static Task SendCloudToDeviceMessageAsync()
        {
            var commandMessage = new Message(Encoding.UTF8.GetBytes("Cloud to device message."));
            await serviceClient.SendAsync("XamarinClient", commandMessage);
        }
    }
}

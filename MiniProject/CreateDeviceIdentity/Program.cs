using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace CreateDeviceIdentity
{
    class Program
    {
        private static RegistryManager registryManager;
        private static string connectionString = "HostName=AseClientHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=vCkk8SgqouOM2JytAgpxRaTRZPFEsrDvl/7Frr2ctl0=";

        static void Main(string[] args)
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            AddDeviceAsync().Wait();
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }

        private static async Task AddDeviceAsync()
        {
            string deviceId = "XamarinClient";
            Device device;
            try
            {
                device = await registryManager.AddDeviceAsync(new Device(deviceId));
            }
            catch (DeviceAlreadyExistsException)
            {
                device = await registryManager.GetDeviceAsync(deviceId);
            }

            Console.WriteLine("Generated device key: {0}", device.Authentication.SymmetricKey.PrimaryKey);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;

namespace Sensor
{
	class Program
	{
		private static EventHubClient eventHubClient;

		//TODO proper configuration
		private const string EhConnectionString =
				"Endpoint=sb://sensorinputevents.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=qZUuYmnG/P7WWjFcQ1V1voFWzL5l8HTsOAj54ozKDGI="
			;

		private const string EhEntityPath = "sensorinput";

		static void Main(string[] args)
		{
			MainAsync(args).GetAwaiter().GetResult();
		}

		private static async Task MainAsync(string[] args)
		{
			var connectionStringBuilder = new EventHubsConnectionStringBuilder(EhConnectionString) { EntityPath = EhEntityPath };

			eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

			await SendMessagesToEventHub();

			await eventHubClient.CloseAsync();
		}

		private static async Task SendMessagesToEventHub()
		{

			//TODO read data from API
			StreamReader reader = new StreamReader("DummyData.json");
			string jsonData = reader.ReadToEnd();

			dynamic deserializedData = JsonConvert.DeserializeObject(jsonData);
			dynamic monitors = deserializedData.data.monitors;
			do
			{
				foreach (dynamic monitor in monitors)
				{
					try
					{
						Console.WriteLine($"Sending message for '{monitor.locationStop.properties.title}'.");
						await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(monitor.ToString()))).ConfigureAwait(false);
						await Task.Delay(TimeSpan.FromMilliseconds(10));
					}
					catch (Exception exception) { Console.WriteLine($"{DateTime.Now} > Exception: {exception.Message}"); }
				}
				Console.WriteLine("Press any key to send again, Type \"Exit\" to close program!");
			}
			while (Console.ReadLine()?.ToLower() != "exit");
		}
	}
}

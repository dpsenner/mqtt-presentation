using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PublishTemperatureAlarms
{
    public class RunCommand
    {
        public double CpuTemperatureThreshold { get; set; }

        public string ApplicationId { get; }

        protected string ApplicationPrefix => $"{ApplicationId}";

        protected string ApplicationCommandPrefix => $"{ApplicationPrefix}/command";

        protected string ApplicationPropertyPrefix => $"{ApplicationPrefix}/property";

        protected IMqttClient MqttClient { get; }

        protected TaskCompletionSource<bool> ShutdownFromRemote { get; }

        protected Dictionary<string, double> Alarms { get; }

        public RunCommand(IMqttClient mqttClient, string applicationId, double cpuTemperatureThreshold)
        {
            MqttClient = mqttClient;
            ApplicationId = applicationId;
            CpuTemperatureThreshold = cpuTemperatureThreshold;
            ShutdownFromRemote = new TaskCompletionSource<bool>();
            Alarms = new Dictionary<string, double>();
        }

        public async Task RunAsync()
        {
            // set up shutdown tasks
            Task<bool> shutdownFromLocalTask = AttachShutdownFromLocalHandler();

            // attach message received event handler
            MqttClient.ApplicationMessageReceived += Client_ApplicationMessageReceived;

            // publish application properties
            await PublishBirth();

            // subscribe to topics: properties
            foreach (var subscribeResult in await MqttClient.SubscribeAsync($"{ApplicationCommandPrefix}/#"))
            {
                Console.WriteLine($"Subscribed to: {subscribeResult.TopicFilter.Topic}");
            }

            // subscribe to topics: commands
            foreach (var subscribeResult in await MqttClient.SubscribeAsync($"{ApplicationPropertyPrefix}/+/set"))
            {
                Console.WriteLine($"Subscribed to: {subscribeResult.TopicFilter.Topic}");
            }

            // subscribe to topics: sensor data
            foreach (var subscribeResult in await MqttClient.SubscribeAsync($"/+/property/temperature/cpu/#"))
            {
                Console.WriteLine($"Subscribed to: {subscribeResult.TopicFilter.Topic}");
            }

            await Task.WhenAny(shutdownFromLocalTask, ShutdownFromRemote.Task);
            Console.WriteLine($"Bye!");
        }

        private async Task PublishBirth()
        {
            await PublishCpuThreshold();
        }

        private async Task PublishCpuThreshold()
        {
            await MqttClient.PublishAsync($"{ApplicationPropertyPrefix}/cpu-threshold", $"{CpuTemperatureThreshold}°C");
        }

        private async void Client_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                await HandleMqttApplicationMessageAsync(e.ApplicationMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unhandled exception occurred while handling a message on topic '{e.ApplicationMessage.Topic}'");
                Console.WriteLine(ex);
            }
        }

        private async Task HandleMqttApplicationMessageAsync(MqttApplicationMessage applicationMessage)
        {
            string topic = applicationMessage.Topic;

            if (topic.StartsWith(ApplicationCommandPrefix))
            {
                if (topic == $"{ApplicationCommandPrefix}/shutdown")
                {
                    string shutdownToken = applicationMessage.ConvertPayloadToString();
                    if (shutdownToken == "very-secret")
                    {
                        ShutdownFromRemote.SetResult(true);
                    }
                    else
                    {
                        Console.WriteLine($"{topic}: refused");
                    }
                }
                else if (topic == $"{ApplicationCommandPrefix}/rebirth")
                {
                    await PublishBirth();
                }
            }
            else if (topic == $"{ApplicationPropertyPrefix}/cpu-threshold/set")
            {
                string payloadAsString = applicationMessage.ConvertPayloadToString();
                double temperature = payloadAsString.ToDouble();
                Console.WriteLine($"{topic}: updated cpu threshold from {CpuTemperatureThreshold}°C to {temperature}°C");
                CpuTemperatureThreshold = temperature;
                await PublishCpuThreshold();
            }
            else if (topic.Contains("/property/temperature/cpu"))
            {
                string remoteApplicationId = Regex.Match(topic, "^/(.+)/").Groups[1].Value;
                string payloadAsString = applicationMessage.ConvertPayloadToString();
                string temperatureAsString = Regex.Replace(payloadAsString, "[^0-9\\.]", "");
                double temperature = temperatureAsString.ToDouble();
                string temperatureUnit = payloadAsString.Replace(temperatureAsString, "");
                await TemperatureReceived(remoteApplicationId, temperature, temperatureUnit);
            }
            else
            {
                Console.WriteLine($"{topic} unhandled");
            }
        }

        private async Task TemperatureReceived(string remoteApplicationId, double temperature, string temperatureUnit)
        {
            switch (temperatureUnit)
            {
                case "°C":
                    if (temperature >= CpuTemperatureThreshold)
                    {
                        await AlarmDetected(remoteApplicationId, temperature, temperatureUnit);
                    }
                    else
                    {
                        await AlarmResolved(remoteApplicationId, temperature, temperatureUnit);
                    }
                    break;
                default:
                    throw new NotImplementedException($"{temperatureUnit} not implemented");
            }
        }

        private async Task AlarmResolved(string remoteApplicationId, double temperature, string temperatureUnit)
        {
            if (!Alarms.ContainsKey(remoteApplicationId))
            {
                // no alarm detected, skip
                return;
            }

            Console.WriteLine($"{remoteApplicationId}: temperature back to normal ({temperature}{temperatureUnit})");
            string topic = $"{ApplicationPropertyPrefix}/alarms/{remoteApplicationId}";
            await MqttClient.PublishAsync(topic, "resolved");
            Alarms.Remove(remoteApplicationId);
        }

        private async Task AlarmDetected(string remoteApplicationId, double temperature, string temperatureUnit)
        {
            if (Alarms.ContainsKey(remoteApplicationId))
            {
                if (Alarms[remoteApplicationId] >= temperature)
                {
                    // keep current alarm
                    return;
                }
            }

            Console.WriteLine($"{remoteApplicationId}: temperature alarm! ({temperature}{temperatureUnit} above {CpuTemperatureThreshold}{temperatureUnit})");
            string topic = $"{ApplicationPropertyPrefix}/alarms/{remoteApplicationId}";
            string payload = $"{temperature}{temperatureUnit} above {CpuTemperatureThreshold}{temperatureUnit}";
            await MqttClient.PublishAsync(topic, payload);
            Alarms[remoteApplicationId] = temperature;
        }

        private static Task<bool> AttachShutdownFromLocalHandler()
        {
            var shutdownFromLocal = new TaskCompletionSource<bool>();
            var shutdownFromLocalTask = shutdownFromLocal.Task;
            Console.CancelKeyPress += (sender, e) =>
            {
                // cancel termination of the current process
                e.Cancel = true;

                // but set the cancellation token to stop the application gracefully
                shutdownFromLocal.SetResult(true);
            };
            return shutdownFromLocalTask;
        }
    }
}

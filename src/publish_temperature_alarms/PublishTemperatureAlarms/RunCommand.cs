using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PublishTemperatureAlarms
{
    public class RunCommand
    {
        public double TemperatureThreshold { get; set; }

        public string ApplicationId { get; }

        protected string ApplicationPrefix => $"{ApplicationId}";

        protected string ApplicationCommandPrefix => $"{ApplicationPrefix}/command";

        protected string ApplicationPropertyPrefix => $"{ApplicationPrefix}/property";

        protected IMqttClient MqttClient { get; }

        protected TaskCompletionSource<bool> ShutdownFromRemote { get; }

        protected List<Alarm> Alarms { get; }

        public RunCommand(IMqttClient mqttClient, string applicationId, double cpuTemperatureThreshold)
        {
            MqttClient = mqttClient;
            ApplicationId = applicationId;
            TemperatureThreshold = cpuTemperatureThreshold;
            ShutdownFromRemote = new TaskCompletionSource<bool>();
            Alarms = new List<Alarm>();
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
            foreach (var subscribeResult in await MqttClient.SubscribeAsync($"+/property/+/temperature"))
            {
                Console.WriteLine($"Subscribed to: {subscribeResult.TopicFilter.Topic}");
            }

            await Task.WhenAny(shutdownFromLocalTask, ShutdownFromRemote.Task);

            Console.WriteLine($"Bye!");
        }

        public MqttApplicationMessage GetLastWill()
        {
            return new MqttApplicationMessageBuilder()
                .WithTopic($"{ApplicationPrefix}/STATE")
                .WithPayload("DEAD")
                .WithRetainFlag()
                .Build();
        }

        private async Task PublishBirth()
        {
            await PublishState();
            await PublishAlarms();
            await PublishCpuThreshold();
        }

        private async Task PublishState()
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"{ApplicationPrefix}/STATE")
                .WithPayload("ALIVE")
                .WithRetainFlag()
                .Build();
            await MqttClient.PublishAsync(message);
        }

        private async Task PublishAlarms(params Alarm[] oldAlarms)
        {
            var messages = Alarms
                .Select(alarm => new MqttApplicationMessageBuilder()
                    .WithTopic($"{ApplicationPropertyPrefix}/alarms/{alarm.ApplicationId}/{alarm.Component}")
                    .WithPayload($"{alarm.Temperature}°C")
                    .Build())
                .Union(oldAlarms.Select(alarm => new MqttApplicationMessageBuilder()
                             .WithTopic($"{ApplicationPropertyPrefix}/alarms/{alarm.ApplicationId}/{alarm.Component}")
                             .WithPayload("resolved")
                             .Build()));
            await MqttClient.PublishAsync(messages);
        }

        private async Task PublishCpuThreshold()
        {
            await MqttClient.PublishAsync($"{ApplicationPropertyPrefix}/temperature-threshold", $"{TemperatureThreshold}°C");
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
            else if (topic == $"{ApplicationPropertyPrefix}/temperature-threshold/set")
            {
                string payloadAsString = applicationMessage.ConvertPayloadToString();
                double temperature = payloadAsString.ToDouble();
                Console.WriteLine($"{topic}: updated temperature threshold from {TemperatureThreshold}°C to {temperature}°C");
                TemperatureThreshold = temperature;
                await PublishCpuThreshold();
            }
            else
            {
                var match = Regex.Match(topic, "^([^/]+)/property/([^/]+)/temperature");
                if (match.Success)
                {
                    string remoteApplicationId = match.Groups[1].Value;
                    string component = match.Groups[2].Value;
                    string payloadAsString = applicationMessage.ConvertPayloadToString();
                    string temperatureAsString = Regex.Replace(payloadAsString, "[^0-9\\.]", "");
                    double temperature = temperatureAsString.ToDouble();
                    await TemperatureReceived(remoteApplicationId, component, temperature);
                }
                else
                {
                    Console.WriteLine($"{topic} unhandled");
                }
            }
        }

        private async Task TemperatureReceived(string remoteApplicationId, string component, double temperature)
        {
            if (temperature >= TemperatureThreshold)
            {
                await TemperatureAboveTreshold(remoteApplicationId, component, temperature);
            }
            else
            {
                await TemperatureBelowThreshold(remoteApplicationId, component, temperature);
            }
        }

        private async Task TemperatureAboveTreshold(string remoteApplicationId, string component, double temperature)
        {
            var alarm = new Alarm()
            {
                ApplicationId = remoteApplicationId,
                Component = component,
                Temperature = temperature,
            };

            if (Alarms.Contains(alarm))
            {
                // keep current alarm
                Console.WriteLine($"{remoteApplicationId}: temperature of {component} is {temperature}°C (still above threshold)");
                return;
            }

            Console.WriteLine($"{remoteApplicationId}: temperature alarm! ({component} {temperature}°C above {TemperatureThreshold}°C)");
            Alarms.Add(alarm);
            await PublishAlarms();
        }

        private async Task TemperatureBelowThreshold(string remoteApplicationId, string component, double temperature)
        {
            var alarm = new Alarm()
            {
                ApplicationId = remoteApplicationId,
                Component = component,
                Temperature = temperature,
            };

            if (!Alarms.Contains(alarm))
            {
                // no alarm detected, skip
                Console.WriteLine($"{remoteApplicationId}: temperature of {component} is {temperature}°C");
                return;
            }

            Console.WriteLine($"{remoteApplicationId}: temperature of {component} back to normal ({temperature}°C)");
            Alarms.Remove(alarm);

            await PublishAlarms(alarm);
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

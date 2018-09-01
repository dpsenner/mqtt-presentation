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

        protected string ApplicationPrefix => $"/{ApplicationId}";

        protected string ApplicationCommandPrefix => $"{ApplicationPrefix}/command";

        protected string ApplicationPropertyPrefix => $"{ApplicationPrefix}/property";

        protected string SensorsTemperaturePrefix => "/sensor/temperature";

        protected IMqttClient MqttClient { get; }

        protected TaskCompletionSource<bool> ShutdownFromRemote { get; }

        public RunCommand(IMqttClient mqttClient, string applicationId, double cpuTemperatureThreshold)
        {
            MqttClient = mqttClient;
            ApplicationId = applicationId;
            CpuTemperatureThreshold = cpuTemperatureThreshold;
            ShutdownFromRemote = new TaskCompletionSource<bool>();
        }

        public async Task RunAsync()
        {
            // set up shutdown tasks
            Task<bool> shutdownFromLocalTask = AttachShutdownFromLocalHandler();

            // attach message received event handler
            MqttClient.ApplicationMessageReceived += Client_ApplicationMessageReceived;

            // publish application properties
            await MqttClient.PublishAsync($"{ApplicationPropertyPrefix}/cpu-threshold", $"{CpuTemperatureThreshold}°C");

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
            foreach (var subscribeResult in await MqttClient.SubscribeAsync($"{SensorsTemperaturePrefix}/#"))
            {
                Console.WriteLine($"Subscribed to: {subscribeResult.TopicFilter.Topic}");
            }

            await Task.WhenAny(shutdownFromLocalTask, ShutdownFromRemote.Task);
            Console.WriteLine($"Bye!");
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
            }
            else if (topic == $"{ApplicationPropertyPrefix}/cpu-threshold/set")
            {
                string payloadAsString = applicationMessage.ConvertPayloadToString();
                double temperature = payloadAsString.ToDouble();
                Console.WriteLine($"{topic}: updated cpu threshold from {CpuTemperatureThreshold}°C to {temperature}°C");
                CpuTemperatureThreshold = temperature;
                await MqttClient.PublishAsync($"{ApplicationPropertyPrefix}/cpu-threshold", $"{CpuTemperatureThreshold}°C");
            }
            else if (topic.StartsWith(SensorsTemperaturePrefix))
            {
                string payloadAsString = applicationMessage.ConvertPayloadToString();
                string temperatureAsString = Regex.Replace(payloadAsString, "[^0-9\\.]", "");
                double temperature = temperatureAsString.ToDouble();
                string temperatureUnit = payloadAsString.Replace(temperatureAsString, "");
                switch (temperatureUnit)
                {
                    case "°C":
                        if (Regex.IsMatch(topic, $"^{SensorsTemperaturePrefix}/.+/cpu/"))
                        {
                            if (temperature >= CpuTemperatureThreshold)
                            {
                                // raise alarm
                                string alarmTopic = topic.Replace("/sensor/", "/alarm/");
                                Console.WriteLine($"{topic}: {temperature}{temperatureUnit} (!!)");
                                await MqttClient.PublishAsync(alarmTopic, payloadAsString);
                            }
                            else
                            {
                                Console.WriteLine($"{topic}: {temperature}{temperatureUnit}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{topic}: {temperature}{temperatureUnit}");
                        }
                        break;
                    default:
                        Console.WriteLine($"{topic}: {temperatureUnit} not implemented");
                        break;
                }
            }
            else
            {
                Console.WriteLine($"{topic} unhandled");
            }
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

using Microsoft.Extensions.CommandLineUtils;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PublishTemperatureAlarms
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CommandLineApplication()
                .AddRunCommand()
                .ShowHelpOnRun()
                .Execute(args);
        }

        private static CommandLineApplication AddRunCommand(this CommandLineApplication application)
        {
            application.Command("run", runCommand =>
            {
                runCommand.Description = "Runs the application.";
                var hostOption = runCommand.Option("-h|--host", "The hostname of the mqtt broker.", CommandOptionType.SingleValue);
                var portOption = runCommand.Option("-p|--port", "The port of the mqtt broker.", CommandOptionType.SingleValue);
                var applicationIdOption = runCommand.Option("-aid|--application-id", "The id of this application.", CommandOptionType.SingleValue);
                var cpuTemperatureThresholdOption = runCommand.Option("-cpu-t|--cpu-threshold", "The temperature threshold in °C that raises an alarm.", CommandOptionType.SingleValue);
                runCommand.OnExecute(async () =>
                {
                    try
                    {
                        string host = "localhost";
                        if (hostOption.HasValue())
                        {
                            host = hostOption.Value();
                        }

                        int port = 1883;
                        if (portOption.HasValue())
                        {
                            port = portOption.Value().ToInt32();
                        }

                        string applicationId = "temperature-alarm";
                        if (applicationIdOption.HasValue())
                        {
                            applicationId = applicationIdOption.Value();
                        }

                        double cpuTemperatureThreshold = 65;
                        if (cpuTemperatureThresholdOption.HasValue())
                        {
                            cpuTemperatureThreshold = cpuTemperatureThresholdOption.Value().ToDouble();
                        }

                        string topicApplicationPrefix = $"/{applicationId}";
                        string topicApplicationCommandPrefix = $"{topicApplicationPrefix}/command";
                        string topicApplicationPropertyPrefix = $"{topicApplicationPrefix}/property";
                        string topicSensorsTemperaturePrefix = "/sensor/temperature";

                        // set up shutdown tasks
                        var shutdownFromRemote = new TaskCompletionSource<bool>();
                        var shutdownFromRemoteTask = shutdownFromRemote.Task;
                        Task<bool> shutdownFromLocalTask = AttachShutdownFromLocalHandler();

                        Console.WriteLine($"Connecting to {host}:{port} ..");
                        var factory = new MqttFactory();
                        using (var client = factory.CreateMqttClient())
                        {
                            var options = new MqttClientOptionsBuilder()
                                .WithTcpServer(host, port)
                                .Build();
                            await client.ConnectAsync(options);

                            // attach message received event handler
                            client.ApplicationMessageReceived += (sender, e) =>
                            {
                                string topic = e.ApplicationMessage.Topic;

                                if (topic.StartsWith(topicApplicationCommandPrefix))
                                {
                                    if (topic == $"{topicApplicationCommandPrefix}/shutdown")
                                    {
                                        string shutdownToken = e.ApplicationMessage.ConvertPayloadToString();
                                        if (shutdownToken == "very-secret")
                                        {
                                            shutdownFromRemote.SetResult(true);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"{topic}: refused");
                                        }
                                    }
                                }
                                else if (topic == $"{topicApplicationPropertyPrefix}/cpu-threshold/set")
                                {
                                    string payloadAsString = e.ApplicationMessage.ConvertPayloadToString();
                                    double temperature = payloadAsString.ToDouble();
                                    Console.WriteLine($"{topic}: updated cpu threshold from {cpuTemperatureThreshold}°C to {temperature}°C");
                                    cpuTemperatureThreshold = temperature;
                                    client.PublishAsync($"{topicApplicationPropertyPrefix}/cpu-threshold", $"{cpuTemperatureThreshold}°C");
                                }
                                else if (topic.StartsWith(topicSensorsTemperaturePrefix))
                                {
                                    string payloadAsString = e.ApplicationMessage.ConvertPayloadToString();
                                    string temperatureAsString = Regex.Replace(payloadAsString, "[^0-9\\.]", "");
                                    double temperature = temperatureAsString.ToDouble();
                                    string temperatureUnit = payloadAsString.Replace(temperatureAsString, "");
                                    switch (temperatureUnit)
                                    {
                                        case "°C":
                                            if (Regex.IsMatch(topic, $"^{topicSensorsTemperaturePrefix}/.+/cpu/"))
                                            {
                                                if (temperature >= cpuTemperatureThreshold)
                                                {
                                                    // raise alarm
                                                    string alarmTopic = topic.Replace("/sensor/", "/alarm/");
                                                    Console.WriteLine($"{topic}: {temperature}{temperatureUnit} (!!)");
                                                    client.PublishAsync(alarmTopic, payloadAsString);
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
                            };

                            // publish application properties
                            await client.PublishAsync($"{topicApplicationPropertyPrefix}/cpu-threshold", $"{cpuTemperatureThreshold}°C");

                            // subscribe to topics: properties
                            foreach (var subscribeResult in await client.SubscribeAsync($"{topicApplicationCommandPrefix}/#"))
                            {
                                Console.WriteLine($"Subscribed to: {subscribeResult.TopicFilter.Topic}");
                            }

                            // subscribe to topics: commands
                            foreach (var subscribeResult in await client.SubscribeAsync($"{topicApplicationPropertyPrefix}/+/set"))
                            {
                                Console.WriteLine($"Subscribed to: {subscribeResult.TopicFilter.Topic}");
                            }

                            // subscribe to topics: sensor data
                            foreach (var subscribeResult in await client.SubscribeAsync($"{topicSensorsTemperaturePrefix}/#"))
                            {
                                Console.WriteLine($"Subscribed to: {subscribeResult.TopicFilter.Topic}");
                            }

                            await Task.WhenAny(shutdownFromLocalTask, shutdownFromRemoteTask);
                            Console.WriteLine($"Bye!");
                        }

                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        return 1;
                    }
                });
            });
            return application;
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
        private static CommandLineApplication ShowHelpOnRun(this CommandLineApplication application)
        {
            application.OnExecute(() =>
            {
                application.ShowHelp();
                return 1;
            });
            return application;
        }

        private static double ToDouble(this string value)
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private static int ToInt32(this string value)
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
    }
}

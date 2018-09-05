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


                        var factory = new MqttFactory();
                        using (var client = factory.CreateMqttClient())
                        {
                            var runCommandContext = new RunCommand(client, applicationId, cpuTemperatureThreshold);
                            Console.WriteLine($"Connecting to {host}:{port} ..");
                            var options = new MqttClientOptionsBuilder()
                                .WithTcpServer(host, port)
                                .WithWillMessage(runCommandContext.GetLastWill())
                                .Build();
                            await client.ConnectAsync(options);
                            await runCommandContext.RunAsync();
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

        private static CommandLineApplication ShowHelpOnRun(this CommandLineApplication application)
        {
            application.OnExecute(() =>
            {
                application.ShowHelp();
                return 1;
            });
            return application;
        }
    }
}

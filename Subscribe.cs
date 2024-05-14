using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Serilog;

class Program
{
    static async Task Main(string[] args)
    {
        var factory = new MqttFactory();
        var clients = new List<IMqttClient>();
        var messageCounts = new Dictionary<int, int>();

        for (int i = 0; i < 10; i++) // Create multiple clients
        {
            var client = factory.CreateMqttClient();
            string clientId = $"mqtt-test-{i}";
            var options = BuildClientOptions("brokerIP", "port", "username", "userpassword", clientId);
            messageCounts[i] = 0;

            ILogger logger = CreateLogger(i);
            clients.Add(client);
            await ConnectAndSubscribeAsync(client, options, "/test", i, logger, messageCounts);
            Console.WriteLine($"Connected to broker, client ID: {i}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();

        WriteMessageCountsToFile(messageCounts);

        await DisconnectAllClients(clients);
    }

    static ILogger CreateLogger(int index)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File($"logs/client{index}/client{index}.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    static IMqttClientOptions BuildClientOptions(string server, int port, string username, string password, string clientId)
    {
        return new MqttClientOptionsBuilder()
            .WithWebSocketServer($"ws://{server}:{port}/mqtt")
            .WithCredentials(username, password)
            .WithClientId(clientId)
            .WithCleanSession(false)
            .WithSessionExpiryInterval(3600)
            .WithCommunicationTimeout(TimeSpan.FromSeconds(30))
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
            .Build();
    }

    static async Task ConnectAndSubscribeAsync(IMqttClient mqttClient, IMqttClientOptions options, string topic, int clientIndex, ILogger logger, Dictionary<int, int> messageCounts)
    {
        mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
            messageCounts[clientIndex]++;
            logger.Information($"Client {clientIndex} received message on topic: {e.ApplicationMessage.Topic}");
            var messagePayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            Console.WriteLine($"Message received: {messagePayload}");
        });

        await mqttClient.ConnectAsync(options);
        await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce).Build());
        logger.Information($"Client {clientIndex} subscribed to {topic}");
    }

    static void WriteMessageCountsToFile(Dictionary<int, int> messageCounts)
    {
        using (var summaryWriter = new StreamWriter("logs/summary.txt"))
        {
            foreach (var count in messageCounts)
            {
                summaryWriter.WriteLine($"Client {count.Key} received {count.Value} messages.");
            }
        }
    }

    static async Task DisconnectAllClients(List<IMqttClient> clients)
    {
        foreach (var client in clients)
        {
            await client.DisconnectAsync();
        }
    }
}

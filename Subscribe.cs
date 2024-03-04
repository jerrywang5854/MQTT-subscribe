using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
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

        // Create and configure multiple MQTT clients
        for (int i = 0; i < 1; i++)
        {
            var client = factory.CreateMqttClient();
            string clientid = $"mqtt-test-{i}";
            var options = BuildClientOptions("brokerIP", 1883, "user1", "321", clientid);   

            messageCounts[i] = 0;

            // Create separate loggers for each client
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File($"logs/client{i}/client{i}_.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            clients.Add(client);
            await ConnectAndSubscribeAsync(client, options, "/test", i, logger, messageCounts); // Use wildcard "#" to subscribe to all topics
            Console.WriteLine($"Connected to broker.client:{i}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();

        // Output the number of messages received by each client to a separate file
        using (var summaryWriter = new StreamWriter("logs/summary.txt"))
        {
            foreach (var count in messageCounts)
            {
                summaryWriter.WriteLine($"Client {count.Key} received {count.Value} messages.");
            }
        }

        // Disconnect all clients
        foreach (var client in clients)
        {
            await client.DisconnectAsync();
        }
    }

    static IMqttClientOptions BuildClientOptions(string server, int port, string username, string password, string clientid)
    {
        return new MqttClientOptionsBuilder()
            .WithWebSocketServer($"ws://{server}:{port}/mqtt")
            .WithCredentials(username, password)
            .WithClientId(clientid)
            .WithCleanSession(false)
            .WithSessionExpiryInterval(3600)
            .WithCommunicationTimeout(TimeSpan.FromSeconds(30))
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311) //Protocol
            .Build();
    }

    static async Task ConnectAndSubscribeAsync(IMqttClient mqttClient, IMqttClientOptions options, string topic, int clientIndex, ILogger logger, Dictionary<int, int> messageCounts)
    {
        int count =0;
        mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
            count++;
            messageCounts[clientIndex]++;
            logger.Information($"Client {clientIndex} received message on topic: {e.ApplicationMessage.Topic}-{count}");
            var messagePayload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            Console.WriteLine($"Client {clientIndex} received message: {messagePayload}-{count}");
            
        });

        // Connect to MQTT broker
        await mqttClient.ConnectAsync(options);

        // Subscribe to topics
        await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce).Build());
        logger.Information($"Client {clientIndex} subscribed to {topic}");
    }
}

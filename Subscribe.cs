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
    // Configuration constants for MQTT connection
    int clientCounts = 10;
    string brokerIP = "yourbrokerIP";
    string port = "yourport";
    string username = "yourname";
    string userpassword = "yourpassword";
    
    static async Task Main(string[] args)
    {
        var factory = new MqttFactory(); // MQTT client factory
        var clients = new List<IMqttClient>(); // List to store multiple MQTT clients
        var messageCounts = new Dictionary<int, int>(); // Dictionary to count messages for each client

        // Initialize multiple MQTT clients
        for (int i = 0; i < clientCounts; i++)
        {
            var client = factory.CreateMqttClient();
            string clientId = $"mqtt-test-{i}";
            // Build connection options for each client
            var options = BuildClientOptions(brokerIP, port, username, userpassword, clientId);
            messageCounts[i] = 0;

            // Create logger specific to each client
            ILogger logger = CreateLogger(i);
            clients.Add(client);
            // Connect and subscribe each client asynchronously
            await ConnectAndSubscribeAsync(client, options, "/test", i, logger, messageCounts);
            Console.WriteLine($"Connected to broker, client ID: {i}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();

        // Write summary of message counts to file
        WriteMessageCountsToFile(messageCounts);

        // Disconnect all clients asynchronously
        await DisconnectAllClients(clients);
    }

    // Creates a logger for a specific client, using Serilog
    static ILogger CreateLogger(int index)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File($"logs/client{index}/client{index}.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    // Builds MQTT client options including WebSocket server details, credentials, and session configuration
    static IMqttClientOptions BuildClientOptions(string server, int port, string username, string password, string clientId)
    {
        return new MqttClientOptionsBuilder()
            .WithWebSocketServer($"ws://{server}:{port}/mqtt")
            .WithCredentials(username, password)
            .WithClientId(clientId)
            .WithCleanSession(false) // Ensures persistent sessions
            .WithSessionExpiryInterval(3600) // Session expires after 1 hour
            .WithCommunicationTimeout(TimeSpan.FromSeconds(30))
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
            .Build();
    }

    // Connects and subscribes an MQTT client to a topic, sets up message handling
    static async Task ConnectAndSubscribeAsync(IMqttClient mqttClient, IMqttClientOptions options, string topic, int clientIndex, ILogger logger, Dictionary<int, int> messageCounts)
    {
        // Setup handler for receiving messages
        mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
            messageCounts[clientIndex]++;
            logger.Information($"Client {clientIndex} received message on topic: {e.ApplicationMessage.Topic}");
            var messagePayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            Console.WriteLine($"Message received: {messagePayload}");
        });

        // Connect and subscribe to the topic with QoS level set to ExactlyOnce
        await mqttClient.ConnectAsync(options);
        await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce).Build());
        logger.Information($"Client {clientIndex} subscribed to {topic}");
    }

    // Writes the number of messages each client received to a summary file
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

    // Disconnects all MQTT clients
    static async Task DisconnectAllClients(List<IMqttClient> clients)
    {
        foreach (var client in clients)
        {
            await client.DisconnectAsync();
        }
    }
}

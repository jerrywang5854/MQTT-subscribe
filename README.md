# Overview

This repository contains a C# application demonstrating the use of multiple MQTT clients to connect to an MQTT broker, subscribe to topics, and log messages received.

## Features

- Connect multiple MQTT clients to an MQTT broker using WebSocket.
- Subscribe to specific topics.
- Log messages received by each client separately.
- Summarize the message count per client upon program termination.

## Prerequisites

Before you begin, ensure you have met the following requirements:
- .NET Core 3.1 SDK or later
- MQTTnet library
- Serilog library for logging

## Usage
Before running the program, users must edit the Subscribe.cs file to specify their own brokerIP, port, username, and userpassword in the BuildClientOptions method.

To run the program, use the following command from the root of the repository:

```bash
dotnet run
```

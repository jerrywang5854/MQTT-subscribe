# MQTT Subscribe Example

This repository contains a C# application demonstrating the use of multiple MQTT clients to connect to an MQTT broker, subscribe to topics, and log messages received.

## Features

- Connect multiple MQTT clients to an MQTT broker.
- Subscribe to specific topics.
- Log messages received by each client separately.
- Summarize the message count per client upon program termination.

## Prerequisites

Before you begin, ensure you have met the following requirements:
- .NET Core 3.1 SDK or later
- MQTTnet library
- Serilog library for logging

## Installation

Clone the repository to your local machine:

```bash
git clone https://github.com/jerrywang5854/MQTT-subscribe
cd mqtt-client-example
```

## Usage
To run the program, use the following command from the root of the repository:

```bash
dotnet run
```

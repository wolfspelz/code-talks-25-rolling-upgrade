using System.Net;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;
using Orleans.Messaging;
using Orleans.Providers.MongoDB.Configuration;
using My.Interfaces;

string mongodbConnection = Environment.GetEnvironmentVariable("MONGODB_CONNECTION") ?? "";
bool useMongoDBMembership = !string.IsNullOrEmpty(mongodbConnection);
bool useLocalhostClustering = !useMongoDBMembership;

Console.WriteLine($"MongoDB Connection: {mongodbConnection}");
Console.WriteLine($"Using Localhost Clustering: {useLocalhostClustering}");
Console.WriteLine($"Using MongoDB Membership: {useMongoDBMembership}");

var host = Host.CreateDefaultBuilder()
    .UseOrleansClient(client =>
    {
        if (useLocalhostClustering)
        {
            client.UseLocalhostClustering();
        }
        else if (useMongoDBMembership)
        {
            client.UseMongoDBClient(mongodbConnection);
            client.UseMongoDBClustering(options =>
            {
                options.DatabaseName = "orleans";
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
            });
        }
        else
        {
            Console.WriteLine("No valid clustering configuration found. Exiting.");
            Environment.Exit(1);
        }

        client.AddMemoryStreams("MyProvider");
    })

    .Build();

await host.StartAsync();

bool ArgIsMissing(string[] tokens, int requiredPosition, string argumentName)
{
    if (tokens.Length < requiredPosition)
    {
        Console.WriteLine($"Missing argument '{argumentName}' at position {requiredPosition}");
        return true;
    }
    return false;
}

try
{
    var clusterClient = host.Services.GetRequiredService<IClusterClient>();

    Console.WriteLine("Commands:");
    Console.WriteLine("  grid-get <id> # Get the color of the grid with the specified id");
    Console.WriteLine("  grid-info <id> # Get the info of the grid with the specified id");

    while (true)
    {
        var line = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(line))
            continue;
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();

        switch (command)
        {
            case "grid-get":
                {
                    if (ArgIsMissing(parts, 2, "id")) continue;
                    var id = parts[1];
                    var value = await clusterClient.GetGrain<IGrid>(id).Get(4, 4);
                    Console.WriteLine($"Grid {id} value: {value}");
                    break;
                }

            case "q":
            case "quit":
            case "exit":
                {
                    Console.WriteLine("Exiting...");
                    return;
                }

            default:
                {
                    Console.WriteLine("Unknown command.");
                    break;
                }
        }
    }
}
finally
{
    await host.StopAsync();
}

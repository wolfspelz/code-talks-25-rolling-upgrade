using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using My.Logic; // This ensures the grains assembly is loaded
using My.StorageProviders;

string mongodbConnection = Environment.GetEnvironmentVariable("MONGODB_CONNECTION") ?? "";
bool useMongoDBMembership = !string.IsNullOrEmpty(mongodbConnection);
bool useLocalhostClustering = !useMongoDBMembership;
int gatewayPort = int.Parse(Environment.GetEnvironmentVariable("GATEWAY_PORT") ?? "30000");
int siloPort = int.Parse(Environment.GetEnvironmentVariable("SILO_PORT") ?? "11111");
bool useLocalhostAddress = bool.Parse(Environment.GetEnvironmentVariable("USE_LOCAL_HOST_ADDRESS") ?? "false");
string dataFolder = Environment.GetEnvironmentVariable("DATA_FOLDER") ?? "../../../data";

Console.WriteLine($"MongoDB Connection: {mongodbConnection}");
Console.WriteLine($"Using Localhost Clustering: {useLocalhostClustering}");
Console.WriteLine($"Using MongoDB Membership: {useMongoDBMembership}");
Console.WriteLine($"Silo Port: {siloPort}");
Console.WriteLine($"Gateway Port: {gatewayPort}");
var ipAddress = useLocalhostAddress ? IPAddress.Loopback : Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
Console.WriteLine($"Gateway IP: {ipAddress}");
Console.WriteLine($"Data Folder: {dataFolder} = {Path.GetFullPath(dataFolder)}");


var host = Host.CreateDefaultBuilder()
    .UseOrleans(siloBuilder =>
    {
        // siloBuilder.Configure<SiloMessagingOptions>(options =>
        // {
        //     // Reduce shutdown timeout (default is 30s)
        //     options.SystemResponseTimeout = TimeSpan.FromSeconds(2);
        //     options.MaxRequestProcessingTime = TimeSpan.FromSeconds(2);
        // })
        // .Configure<ClusterMembershipOptions>(options =>
        // {
        //     // Faster death declaration
        //     options.ProbeTimeout = TimeSpan.FromSeconds(1);
        //     options.TableRefreshTimeout = TimeSpan.FromSeconds(1);
        // });

        if (useLocalhostClustering)
        {
            siloBuilder.UseLocalhostClustering();
            siloBuilder.ConfigureEndpoints(advertisedIP: ipAddress, siloPort: siloPort, gatewayPort: gatewayPort, true);
        }
        else if (useMongoDBMembership)
        {
            siloBuilder.UseMongoDBClient(mongodbConnection);
            siloBuilder.UseMongoDBClustering(options =>
            {
                options.DatabaseName = "orleans";
                options.Strategy = MongoDBMembershipStrategy.SingleDocument;
            });

            siloBuilder.ConfigureEndpoints(advertisedIP: ipAddress, siloPort: siloPort, gatewayPort: gatewayPort, true);
        }
        else
        {
            Console.WriteLine("No valid clustering configuration found. Exiting.");
            Environment.Exit(1);
        }

        siloBuilder.AddMemoryStreams("MyProvider");
        siloBuilder.AddMemoryGrainStorage("PubSubStore");

        siloBuilder.AddJsonFileStorage("JsonFileStorage", options =>
        {
            options.RootDirectory = dataFolder;
            options.FileExtension = ".json";
            options.IndentJson = true;
            options.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None;
        });


        siloBuilder.Configure<SiloMessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromSeconds(3); // Reduced from 30s
            });

        siloBuilder.ConfigureLogging(logging =>
        {
            logging.AddSimpleConsole(scfo =>
            {
                scfo.SingleLine = true;
                scfo.TimestampFormat = "[yy:MM:dd-HH:mm:ss] ";
            });

            logging.AddFilter((provider, category, level) =>
            {
                if (category is not null && category.StartsWith("Orleans.", StringComparison.Ordinal) && level <= LogLevel.Warning)
                {
                    return false;
                }
                return true;
            });
        });

    })

    .ConfigureServices(services =>
    {
        // Register the hosted service
        // services.AddHostedService<TrackingViewLifecycle>();

        // Optional: Add any other services you need
        // services.AddSingleton<IYourService, YourServiceImplementation>();
    })
    .Build();

// // Handle shutdown signals
// var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
// lifetime.ApplicationStopping.Register(() =>
// {
//     Console.WriteLine("Fast shutdown initiated...");
//     // Skip graceful deactivation for demo
//     Environment.Exit(0);
// });
    
host.Run();
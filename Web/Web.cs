using System.Net;
using System.Net.Sockets;
using Microsoft.CodeAnalysis;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Orleans Client
string mongodbConnection = Environment.GetEnvironmentVariable("MONGODB_CONNECTION") ?? "";

Console.WriteLine($"MongoDB Connection: {mongodbConnection}");

builder.Host.UseOrleansClient(clientBuilder =>
{
    clientBuilder.UseMongoDBClient(mongodbConnection);
    clientBuilder.UseMongoDBClustering(options =>
    {
        options.DatabaseName = "orleans";
        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
    });

    clientBuilder.Configure<ClientMessagingOptions>(options =>
    {
        options.ResponseTimeout = TimeSpan.FromSeconds(30);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Redirect root to Index page
app.MapGet("/", () => Results.Redirect("/Index"));

app.MapRazorPages();
app.MapControllers();

app.Run();
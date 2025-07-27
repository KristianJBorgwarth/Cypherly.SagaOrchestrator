using System.Reflection;
using SagaOrchestrator.API.Extensions;
using SagaOrchestrator.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);



var env = builder.Environment;

// Configuration 
var configuration = builder.Configuration;
configuration.AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables();

if (env.IsDevelopment())
{
    configuration.AddJsonFile($"appsettings.{Environments.Development}.json", true, true);
    configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
}

// Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

builder.Services.AddObservability(configuration);
builder.Host.UseSerilog();

// Infrastructure
builder.Services.AddInfrastructure(configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

public partial class Program { }
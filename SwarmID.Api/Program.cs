using SwarmID.Core.Interfaces;
using SwarmID.Core.Algorithms;
using SwarmID.Core.Repositories;
using SwarmID.Core.Models;
using SwarmID.TrafficAnalysis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register application services
builder.Services.AddSingleton<IAnomalyRepository>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("LiteDB") 
        ?? "Data/swarmid.db";
    return new LiteDbAnomalyRepository(connectionString);
});

// Register Ant Colony Optimization detector
builder.Services.AddTransient<AntColonyOptimizationDetector>();

// Register Bee Algorithm detector
builder.Services.AddTransient<BeeAlgorithmDetector>();

// Register the primary swarm detector (can be configured to use either algorithm)
builder.Services.AddSingleton<ISwarmDetector>(provider =>
{
    var repository = provider.GetRequiredService<IAnomalyRepository>();
    
    // Default to Ant Colony Optimization - could be configured via appsettings
    var useAco = builder.Configuration.GetValue<bool>("SwarmAlgorithm:UseAntColony", true);
    
    return useAco 
        ? new AntColonyOptimizationDetector(repository)
        : new BeeAlgorithmDetector(repository);
});

builder.Services.AddSingleton<ITrafficCollector, ZeekLogParser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

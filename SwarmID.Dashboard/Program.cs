using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SwarmID.Core.Interfaces;
using SwarmID.Core.Algorithms;
using SwarmID.Core.Repositories;
using SwarmID.Core.Models;
using SwarmID.TrafficAnalysis;
using SwarmID.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

// Configure HTTP client for API calls
builder.Services.AddHttpClient("SwarmIDApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7001/"); // API URL
});

// Register application services (same as API)
builder.Services.AddSingleton<IAnomalyRepository>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("LiteDB") 
        ?? "Data/swarmid.db";
    return new LiteDbAnomalyRepository(connectionString);
});

// Register swarm intelligence algorithms
builder.Services.AddScoped<AntColonyOptimizationDetector>();
builder.Services.AddScoped<BeeAlgorithmDetector>();
builder.Services.AddScoped<ParticleSwarmOptimizationDetector>();

// Register algorithm selection service
builder.Services.AddSingleton<IAlgorithmSelectionService, AlgorithmSelectionService>();

// Register dynamic swarm detector that uses the algorithm selection service
builder.Services.AddScoped<ISwarmDetector>(provider =>
{
    var algorithmService = provider.GetRequiredService<IAlgorithmSelectionService>();
    return algorithmService.GetCurrentDetector();
});

builder.Services.AddSingleton<ITrafficCollector, ZeekLogParser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

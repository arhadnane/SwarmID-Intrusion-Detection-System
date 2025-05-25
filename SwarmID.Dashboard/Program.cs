using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SwarmID.Core.Interfaces;
using SwarmID.Core.Algorithms;
using SwarmID.Core.Repositories;
using SwarmID.Core.Models;
using SwarmID.TrafficAnalysis;

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

builder.Services.AddSingleton<ISwarmDetector>(provider =>
{
    var repository = provider.GetRequiredService<IAnomalyRepository>();
    var config = new SwarmConfiguration
    {
        NumberOfAnts = 50,
        MaxIterations = 100,
        PheromoneEvaporationRate = 0.1,
        AnomalyThreshold = 70.0
    };
    return new AntColonyOptimizationDetector(repository);
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

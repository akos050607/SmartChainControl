using Microsoft.AspNetCore.ResponseCompression;
using SmartChainControl.Server.Hubs;
using SmartChainControl.Server.Services;
using SmartChainControl.Server.Workers;

var builder = WebApplication.CreateBuilder(args);

// Configure SignalR with MessagePack for efficient binary serialization
builder.Services.AddSignalR()
       .AddMessagePackProtocol();
builder.Services.AddSingleton<SimulationManager>();
builder.Services.AddHostedService<GameLoopWorker>();

// Allow all origins for development - restrict in production
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true) 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// Enable response compression for SignalR binary messages
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseResponseCompression();
app.UseCors();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();          

app.UseRouting();
app.UseAuthorization();

app.MapHub<WarehouseHub>("/warehousehub");

app.MapFallbackToFile("index.html"); 

app.Run();

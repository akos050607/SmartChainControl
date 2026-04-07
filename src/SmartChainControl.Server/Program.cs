using Microsoft.AspNetCore.ResponseCompression;
using SmartChainControl.Server.Hubs;
using SmartChainControl.Server.Services;
using SmartChainControl.Server.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR()
       .AddMessagePackProtocol();

builder.Services.AddSingleton<SimulationManager>();
builder.Services.AddHostedService<GameLoopWorker>();

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

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection(); // Disabled for Docker/Reverse Proxy compatibility

app.UseResponseCompression();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseCors();
app.UseAuthorization();

app.MapHub<WarehouseHub>("/warehousehub");
app.MapFallbackToFile("index.html"); 

app.Run();
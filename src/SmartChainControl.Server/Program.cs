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
        policy.WithOrigins("https://localhost:7120", "http://localhost:5120")
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

app.MapControllers();

app.UseResponseCompression();
app.UseCors();

app.MapHub<WarehouseHub>("/warehousehub");

app.Run();

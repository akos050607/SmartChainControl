using Microsoft.AspNetCore.SignalR;
using SmartChainControl.Server.Hubs;
using SmartChainControl.Server.Services;

namespace SmartChainControl.Server.Workers;

public class GameLoopWorker : BackgroundService
{
    private readonly SimulationManager _simulationManager;
    private readonly IHubContext<WarehouseHub> _hubContext;
    private readonly PeriodicTimer _timer;

    public GameLoopWorker(SimulationManager simulationManager, IHubContext<WarehouseHub> hubContext)
    {
        _simulationManager = simulationManager;
        _hubContext = hubContext;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(40));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            _simulationManager.Update();
            await _hubContext.Clients.All.SendAsync("ReceiveState", _simulationManager.Robots, stoppingToken);
        }
    }
}
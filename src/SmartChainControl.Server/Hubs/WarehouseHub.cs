using Microsoft.AspNetCore.SignalR;
using SmartChainControl.Server.Services;
using SmartChainControl.Shared.Models;

namespace SmartChainControl.Server.Hubs;

/// <summary>
/// SignalR hub for real-time warehouse simulation updates
/// </summary>
public class WarehouseHub : Hub
{
    private readonly SimulationManager _simulationManager;

    public WarehouseHub(SimulationManager simulationManager)
    {
        _simulationManager = simulationManager;
    }

    // Send initial map and robot state when client connects
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ReceiveMap", _simulationManager.Map);
        await Clients.Caller.SendAsync("ReceiveState", _simulationManager.Robots);
        await base.OnConnectedAsync();
    }
    public Task ToggleManualMode(int robotId, bool enable)
    {
        _simulationManager.ToggleManualMode(robotId, enable);
        return Task.CompletedTask;
    }

    public async Task ManualMoveCommand(int robotId, int targetX, int targetY)
    {
        _simulationManager.SetManualTarget(robotId, targetX, targetY);
    }
}
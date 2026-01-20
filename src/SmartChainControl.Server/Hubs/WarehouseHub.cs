using Microsoft.AspNetCore.SignalR;
using SmartChainControl.Server.Services;
using SmartChainControl.Shared.Models;

namespace SmartChainControl.Server.Hubs;

public class WarehouseHub : Hub
{
    private readonly SimulationManager _simulationManager;

    public WarehouseHub(SimulationManager simulationManager)
    {
        _simulationManager = simulationManager;
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ReceiveMap", _simulationManager.Map);
        await Clients.Caller.SendAsync("ReceiveState", _simulationManager.Robots);
        await base.OnConnectedAsync();
    }
}
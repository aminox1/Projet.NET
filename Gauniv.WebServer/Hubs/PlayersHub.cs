using System.Security.Claims;
using System.Threading.Tasks;
using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gauniv.WebServer.Hubs
{
    [Authorize]
    public class PlayersHub : Hub
    {
        private readonly PlayerPresenceService _presence;

        public PlayersHub(PlayerPresenceService presence)
        {
            _presence = presence;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var display = Context.User?.Identity?.Name ?? "Player";
            if (!string.IsNullOrEmpty(userId))
            {
                await _presence.OnConnectedAsync(userId, Context.ConnectionId, display);
                await Clients.Caller.SendAsync("PlayersUpdated", _presence.GetAll());
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            await _presence.OnDisconnectedAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SetStatus(string status)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await _presence.SetStatusAsync(userId, status);
            }
        }
    }
}


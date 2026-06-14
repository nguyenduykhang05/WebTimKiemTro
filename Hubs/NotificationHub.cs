using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SmartRoomFinder.Services;

namespace SmartRoomFinder.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IUserConnectionManager _connectionManager;

        public NotificationHub(IUserConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                // We reuse the UserConnectionManager to map this new connection ID
                // Note: SignalR creates different ConnectionIds for different hubs even on the same page.
                _connectionManager.KeepUserConnection(userId, Context.ConnectionId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.RemoveUserConnection(Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using Cursework.Api.Hubs;
using Cursework.Application.Realtime;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.SignalR;

namespace Cursework.Api.Realtime;

public class CallWaiterNotifications : ICallWaiterNotifications
{
    private readonly IHubContext<NotificationsHub, IAdminNotificationsClient> _hub;

    public CallWaiterNotifications(
        IHubContext<NotificationsHub, IAdminNotificationsClient> hub)
    {
        _hub = hub;
    }

    public Task NotifyCallWaiterChangedAsync(
        EntityChangeAction action,
        CallWaiter callWaiter,
        CancellationToken ct = default)
    {
        var dto = new CallWaiterChangedDto
        {
            Action = action,
            CallWaiter = callWaiter
        };

        return _hub.Clients.All.CallWaiterChanged(dto);
    }
}

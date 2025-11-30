using System.Threading;
using System.Threading.Tasks;
using Cursework.Api.Hubs;
using Cursework.Application.Realtime;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.SignalR;

namespace Cursework.Api.Realtime;

public class OrderNotifications : IOrderNotifications
{
    private readonly IHubContext<NotificationsHub, IAdminNotificationsClient> _hub;

    public OrderNotifications(
        IHubContext<NotificationsHub, IAdminNotificationsClient> hub)
    {
        _hub = hub;
    }

    public Task NotifyOrderChangedAsync(
        EntityChangeAction action,
        Order order,
        CancellationToken ct = default)
    {
        var dto = new OrderChangedDto
        {
            Action = action,
            Order = order
        };

        return _hub.Clients.All.OrderChanged(dto);
    }
}

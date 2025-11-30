using System.Threading;
using System.Threading.Tasks;
using Cursework.Api.Hubs;
using Cursework.Application.Realtime;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.SignalR;

namespace Cursework.Api.Realtime;

public class TableNotifications : ITableNotifications
{
    private readonly IHubContext<NotificationsHub, IAdminNotificationsClient> _hub;

    public TableNotifications(
        IHubContext<NotificationsHub, IAdminNotificationsClient> hub)
    {
        _hub = hub;
    }

    public Task NotifyTableChangedAsync(
        EntityChangeAction action,
        DiningTable table,
        CancellationToken ct = default)
    {
        var dto = new DiningTableChangedDto
        {
            Action = action,
            Table = table
        };

        return _hub.Clients.All.DiningTableChanged(dto);
    }
}

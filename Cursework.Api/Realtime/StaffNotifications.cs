using System.Threading;
using System.Threading.Tasks;
using Cursework.Api.Hubs;
using Cursework.Application.Realtime;
using Cursework.Domains.Models;
using Microsoft.AspNetCore.SignalR;

namespace Cursework.Api.Realtime;

public class StaffNotifications : IStaffNotifications
{
    private readonly IHubContext<NotificationsHub, IAdminNotificationsClient> _hub;

    public StaffNotifications(
        IHubContext<NotificationsHub, IAdminNotificationsClient> hub)
    {
        _hub = hub;
    }

    public Task NotifyStaffChangedAsync(
        EntityChangeAction action,
        Staff staff,
        CancellationToken ct = default)
    {
        var dto = new StaffChangedDto
        {
            Action = action,
            Staff = staff
        };

        return _hub.Clients.All.StaffChanged(dto);
    }
}

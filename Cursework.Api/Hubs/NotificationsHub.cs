using System.Threading.Tasks;
using Cursework.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Cursework.Api.Hubs;

public interface IAdminNotificationsClient
{
    Task OrderChanged(OrderChangedDto notification);
    Task CallWaiterChanged(CallWaiterChangedDto notification);
    Task DiningTableChanged(DiningTableChangedDto notification);
    Task StaffChanged(StaffChangedDto notification);
}

public class NotificationsHub : Hub<IAdminNotificationsClient>
{
}

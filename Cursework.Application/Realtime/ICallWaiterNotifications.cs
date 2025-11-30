using System.Threading;
using System.Threading.Tasks;
using Cursework.Domains.Models;

namespace Cursework.Application.Realtime;

public interface ICallWaiterNotifications
{
    Task NotifyCallWaiterChangedAsync(
        EntityChangeAction action,
        CallWaiter callWaiter,
        CancellationToken ct = default);
}

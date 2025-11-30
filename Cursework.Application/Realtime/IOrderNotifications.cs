using System.Threading;
using System.Threading.Tasks;
using Cursework.Domains.Models;

namespace Cursework.Application.Realtime;

public interface IOrderNotifications
{
    Task NotifyOrderChangedAsync(
        EntityChangeAction action,
        Order order,
        CancellationToken ct = default);
}

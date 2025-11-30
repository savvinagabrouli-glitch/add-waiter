using System.Threading;
using System.Threading.Tasks;
using Cursework.Domains.Models;

namespace Cursework.Application.Realtime;

public interface ITableNotifications
{
    Task NotifyTableChangedAsync(
        EntityChangeAction action,
        DiningTable table,
        CancellationToken ct = default);
}

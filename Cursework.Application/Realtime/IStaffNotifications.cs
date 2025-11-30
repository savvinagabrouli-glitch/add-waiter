using System.Threading;
using System.Threading.Tasks;
using Cursework.Domains.Models;

namespace Cursework.Application.Realtime;

public interface IStaffNotifications
{
    Task NotifyStaffChangedAsync(
        EntityChangeAction action,
        Staff staff,
        CancellationToken ct = default);
}

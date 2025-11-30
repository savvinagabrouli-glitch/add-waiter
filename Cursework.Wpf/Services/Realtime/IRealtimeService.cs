using System;
using System.Threading;
using System.Threading.Tasks;
using Cursework.Application.Realtime;

namespace Cursework.Wpf.Services.Realtime
{
    public interface IRealtimeService : IAsyncDisposable
    {
        Task StartAsync(CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);

        bool IsConnected { get; }

        event Action<OrderChangedDto> OrderChanged;
        event Action<CallWaiterChangedDto> CallWaiterChanged;
        event Action<DiningTableChangedDto> DiningTableChanged;
        event Action<StaffChangedDto> StaffChanged;
    }
}

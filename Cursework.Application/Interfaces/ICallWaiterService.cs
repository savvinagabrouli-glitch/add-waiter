using Cursework.Domains.Models;

namespace Cursework.Application.Interfaces
{
    public interface ICallWaiterService
    {
        Task<List<CallWaiter>> GetAllAsync(CancellationToken ct = default);
        Task<CallWaiter> AddAsync(CallWaiter callWaiter, CancellationToken ct = default);
        Task UpdateAsync(CallWaiter callWaiter, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}

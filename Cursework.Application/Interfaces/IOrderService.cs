using Cursework.Domains.Models;

namespace Cursework.Application.Interfaces
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllAsync(CancellationToken ct = default);
        Task<Order> AddAsync(Order order, CancellationToken ct = default);
        Task UpdateAsync(Order order, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<bool> HasActiveOrderForTableAsync(int tableId, CancellationToken ct = default);
    }
}

using Cursework.Domains.Models;

namespace Cursework.Application.Interfaces
{
    public interface ITableService
    {
        Task<List<DiningTable>> GetAllAsync(CancellationToken ct = default);
        Task<DiningTable> AddAsync(DiningTable table, CancellationToken ct = default);
        Task UpdateAsync(DiningTable table, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}

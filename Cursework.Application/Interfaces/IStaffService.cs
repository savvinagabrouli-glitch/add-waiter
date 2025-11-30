using Cursework.Domains.Models;

namespace Cursework.Application.Interfaces
{
    public interface IStaffService
    {
        Task<List<Staff>> GetAllAsync(CancellationToken ct = default);
        Task<Staff> AddAsync(Staff staff, CancellationToken ct = default);
        Task UpdateAsync(Staff staff, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}

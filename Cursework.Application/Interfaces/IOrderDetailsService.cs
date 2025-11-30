using Cursework.Application.Models;

namespace Cursework.Application.Interfaces
{
    public interface IOrderDetailsService
    {
        Task<OrderDetailsDto?> GetAsync(int orderId, CancellationToken ct = default);

        Task<OrderDetailsDto> SaveAsync(OrderDetailsDto details, CancellationToken ct = default);

        Task DeleteAsync(int orderId, CancellationToken ct = default);

        Task<byte[]> GetReceiptPdfAsync(int orderId, CancellationToken ct = default);
    }
}

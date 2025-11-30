using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Cursework.Application.Interfaces;
using Cursework.Application.Models;

namespace Cursework.Application.Services.Api
{
    public class ApiOrderDetailsService : IOrderDetailsService
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "api/orderdetails";

        public ApiOrderDetailsService(HttpClient http)
        {
            _http = http;
        }

        public async Task<OrderDetailsDto?> GetAsync(int orderId, CancellationToken ct = default)
        {
            var response = await _http.GetAsync($"{BaseUrl}/{orderId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<OrderDetailsDto>(cancellationToken: ct);
        }

        public async Task<OrderDetailsDto> SaveAsync(OrderDetailsDto details, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync(BaseUrl, details, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OrderDetailsDto>(cancellationToken: ct);
            if (result == null)
                throw new InvalidOperationException("Пустой ответ при сохранении деталей заказа.");

            return result;
        }

        public async Task DeleteAsync(int orderId, CancellationToken ct = default)
        {
            var response = await _http.DeleteAsync($"{BaseUrl}/{orderId}", ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task<byte[]> GetReceiptPdfAsync(int orderId, CancellationToken ct = default)
        {
            var response = await _http.GetAsync($"{BaseUrl}/{orderId}/receipt", ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
    }
}

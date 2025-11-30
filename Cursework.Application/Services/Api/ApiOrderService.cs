using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cursework.Application.Services.Api
{
    public class ApiOrderService : IOrderService
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "api/orders";

        public ApiOrderService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
        {
            var result = await _http.GetFromJsonAsync<List<Order>>(BaseUrl, ct);
            return result ?? new List<Order>();
        }

        public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync(BaseUrl, order, ct);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Order>(cancellationToken: ct);
                if (created == null)
                    throw new InvalidOperationException("Не удалось прочитать созданный заказ из ответа API.");

                return created;
            }

            await HandleOrderErrorAsync(response, "создании заказа", ct);
            return order;
        }

        public async Task UpdateAsync(Order order, CancellationToken ct = default)
        {
            var response = await _http.PutAsJsonAsync($"{BaseUrl}/{order.Id}", order, ct);

            if (response.IsSuccessStatusCode)
                return;

            await HandleOrderErrorAsync(response, "обновлении заказа", ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var response = await _http.DeleteAsync($"{BaseUrl}/{id}", ct);
            response.EnsureSuccessStatusCode();
        }
        private sealed class ErrorResponse
        {
            public string? Message { get; set; }
        }
        private async Task HandleOrderErrorAsync(HttpResponseMessage response, string operation, CancellationToken ct)
        {
            var json = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                try
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (!string.IsNullOrWhiteSpace(error?.Message))
                        throw new InvalidOperationException(error.Message);
                }
                catch (JsonException)
                {
                    if (!string.IsNullOrWhiteSpace(json))
                        throw new InvalidOperationException(json);
                }

                throw new InvalidOperationException($"Ошибка при {operation}: сервер вернул BadRequest.");
            }

            throw new Exception(
                $"Ошибка при {operation}. Код: {(int)response.StatusCode} ({response.StatusCode})");
        }

        public Task<bool> HasActiveOrderForTableAsync(int tableId, CancellationToken ct = default)
        {
            return Task.FromResult(false);
        }
    }
}

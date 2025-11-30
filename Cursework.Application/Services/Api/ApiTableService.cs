using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cursework.Application.Services.Api
{
    public class ApiTableService : ITableService
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "api/tables";

        public ApiTableService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<DiningTable>> GetAllAsync(CancellationToken ct = default)
        {
            var result = await _http.GetFromJsonAsync<List<DiningTable>>(BaseUrl, ct);
            return result ?? new List<DiningTable>();
        }

        public async Task<DiningTable> AddAsync(DiningTable table, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync(BaseUrl, table, ct);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<DiningTable>(cancellationToken: ct);
                if (created == null)
                    throw new InvalidOperationException("Backend вернул пустой ответ при создании стола.");

                return created;
            }

            await HandleTableErrorAsync(response, "создании стола", ct);
            return table;
        }

        public async Task UpdateAsync(DiningTable table, CancellationToken ct = default)
        {
            var response = await _http.PutAsJsonAsync($"{BaseUrl}/{table.Id}", table, ct);

            if (response.IsSuccessStatusCode)
                return;

            await HandleTableErrorAsync(response, "обновлении стола", ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var response = await _http.DeleteAsync($"{BaseUrl}/{id}", ct);

            if (response.IsSuccessStatusCode)
                return;

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var json = await response.Content.ReadAsStringAsync(ct);

                try
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (!string.IsNullOrWhiteSpace(error?.Message))
                        throw new InvalidOperationException(error.Message);
                }
                catch (JsonException)
                {
                    if (!string.IsNullOrWhiteSpace(json))
                        throw new InvalidOperationException(json);
                }

                throw new InvalidOperationException("Не удалось удалить стол: сервер вернул BadRequest.");
            }

            throw new Exception($"Ошибка при удалении стола. Код: {(int)response.StatusCode} ({response.StatusCode})");
        }
        private sealed class ErrorResponse
        {
            public string? Message { get; set; }
        }
        private async Task HandleTableErrorAsync(HttpResponseMessage response, string operation, CancellationToken ct)
        {
            var json = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                try
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

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

            throw new Exception($"Ошибка при {operation}. Код: {(int)response.StatusCode} ({response.StatusCode})");
        }
    }
}

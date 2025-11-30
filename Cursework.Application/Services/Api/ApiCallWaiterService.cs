using System.Net.Http;
using System.Net.Http.Json;
using Cursework.Application.Interfaces;
using Cursework.Domains.Models;

namespace Cursework.Application.Services.Api
{
    public class ApiCallWaiterService : ICallWaiterService
    {
        private readonly HttpClient _http;

        private const string BaseUrl = "api/callwaiter";

        public ApiCallWaiterService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<CallWaiter>> GetAllAsync(CancellationToken ct = default)
        {
            var result = await _http.GetFromJsonAsync<List<CallWaiter>>(BaseUrl, ct);
            return result ?? new List<CallWaiter>();
        }

        public async Task<CallWaiter> AddAsync(CallWaiter callWaiter, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync(BaseUrl, callWaiter, ct);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<CallWaiter>(cancellationToken: ct);
            if (created == null)
                throw new InvalidOperationException("API вернул пустой ответ при создании вызова официанта.");

            return created;
        }

        public async Task UpdateAsync(CallWaiter callWaiter, CancellationToken ct = default)
        {
            var response = await _http.PutAsJsonAsync($"{BaseUrl}/{callWaiter.Id}", callWaiter, ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var response = await _http.DeleteAsync($"{BaseUrl}/{id}", ct);
            response.EnsureSuccessStatusCode();
        }
    }
}

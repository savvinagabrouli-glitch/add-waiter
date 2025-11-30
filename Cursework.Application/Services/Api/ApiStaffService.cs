using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cursework.Application.Services.Api
{
    public class ApiStaffService : IStaffService
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "api/staff";

        public ApiStaffService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<Staff>> GetAllAsync(CancellationToken ct = default)
        {
            var result = await _http.GetFromJsonAsync<List<Staff>>(BaseUrl, ct);
            return result ?? new List<Staff>();
        }

        public async Task<Staff> AddAsync(Staff staff, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync(BaseUrl, staff, ct);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<Staff>(cancellationToken: ct);
                if (created == null)
                    throw new InvalidOperationException("Backend вернул пустой ответ при создании сотрудника.");

                staff.Id = created.Id;
                staff.Name = created.Name;
                staff.Login = created.Login;
                staff.Role = created.Role;
                staff.PasswordHash = created.PasswordHash;

                return staff;
            }

            await HandleStaffErrorAsync(response, "создании сотрудника", ct);
            return staff;
        }


        public async Task UpdateAsync(Staff staff, CancellationToken ct = default)
        {
            var response = await _http.PutAsJsonAsync($"{BaseUrl}/{staff.Id}", staff, ct);

            if (response.IsSuccessStatusCode)
                return;

            await HandleStaffErrorAsync(response, "обновлении сотрудника", ct);
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

                throw new InvalidOperationException("Не удалось удалить сотрудника: сервер вернул BadRequest.");
            }

            throw new Exception($"Ошибка при удалении сотрудника. Код: {(int)response.StatusCode} ({response.StatusCode})");
        }
        private sealed class ErrorResponse
        {
            public string? Message { get; set; }
        }
        private async Task HandleStaffErrorAsync(HttpResponseMessage response, string operation, CancellationToken ct)
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

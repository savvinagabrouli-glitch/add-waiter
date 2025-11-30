using System;
using System.Threading;
using System.Threading.Tasks;
using Cursework.Application.Realtime;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Cursework.Wpf.Services.Realtime
{
    public class SignalRRealtimeService : IRealtimeService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SignalRRealtimeService> _logger;

        private readonly object _lock = new();
        private HubConnection? _connection;

        public bool IsConnected =>
            _connection?.State == HubConnectionState.Connected;

        public event Action<OrderChangedDto>? OrderChanged;
        public event Action<CallWaiterChangedDto>? CallWaiterChanged;
        public event Action<DiningTableChangedDto>? DiningTableChanged;
        public event Action<StaffChangedDto>? StaffChanged;

        public SignalRRealtimeService(
            IHttpClientFactory httpClientFactory,
            ILogger<SignalRRealtimeService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (_connection != null &&
                    (_connection.State == HubConnectionState.Connected
                     || _connection.State == HubConnectionState.Connecting
                     || _connection.State == HubConnectionState.Reconnecting))
                {
                    return;
                }

                if (_connection == null)
                {
                    _connection = BuildConnection();
                    RegisterHandlers(_connection);
                }
            }

            try
            {
                await _connection!.StartAsync(ct);
                _logger.LogInformation("SignalR connection started. State={State}", _connection.State);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start SignalR connection");

                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    try
                    {
                        await _connection!.StartAsync(ct);
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Retry start SignalR connection failed");
                    }
                }, ct);
            }
        }

        public async Task StopAsync(CancellationToken ct = default)
        {
            HubConnection? conn;
            lock (_lock)
            {
                conn = _connection;
            }

            if (conn != null)
            {
                try
                {
                    await conn.StopAsync(ct);
                    _logger.LogInformation("SignalR connection stopped");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping SignalR connection");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            HubConnection? conn;
            lock (_lock)
            {
                conn = _connection;
                _connection = null;
            }

            if (conn != null)
            {
                try
                {
                    await conn.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing SignalR connection");
                }
            }
        }

        private HubConnection BuildConnection()
        {
            var httpClient = _httpClientFactory.CreateClient("Backend");

            if (httpClient.BaseAddress == null)
                throw new InvalidOperationException("HttpClient 'Backend' must have BaseAddress configured.");

            var hubUri = new Uri(httpClient.BaseAddress, "hubs/notifications");

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUri)
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                })
                .Build();

            connection.Reconnecting += error =>
            {
                _logger.LogWarning(error, "SignalR reconnecting...");
                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                _logger.LogInformation("SignalR reconnected. ConnectionId={ConnectionId}", connectionId);
                return Task.CompletedTask;
            };

            connection.Closed += error =>
            {
                _logger.LogWarning(error, "SignalR connection closed");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        await connection.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to restart SignalR connection after close");
                    }
                });

                return Task.CompletedTask;
            };

            return connection;
        }

        private void RegisterHandlers(HubConnection connection)
        {
            connection.On<OrderChangedDto>("OrderChanged", dto =>
            {
                _logger.LogDebug("Received OrderChanged: {Action}, Id={Id}", dto.Action, dto.Order?.Id);
                OrderChanged?.Invoke(dto);
            });

            connection.On<CallWaiterChangedDto>("CallWaiterChanged", dto =>
            {
                _logger.LogDebug("Received CallWaiterChanged: {Action}, Id={Id}", dto.Action, dto.CallWaiter?.Id);
                CallWaiterChanged?.Invoke(dto);
            });

            connection.On<DiningTableChangedDto>("DiningTableChanged", dto =>
            {
                _logger.LogDebug("Received DiningTableChanged: {Action}, Id={Id}", dto.Action, dto.Table?.Id);
                DiningTableChanged?.Invoke(dto);
            });

            connection.On<StaffChangedDto>("StaffChanged", dto =>
            {
                _logger.LogDebug("Received StaffChanged: {Action}, Id={Id}", dto.Action, dto.Staff?.Id);
                StaffChanged?.Invoke(dto);
            });
        }
    }
}

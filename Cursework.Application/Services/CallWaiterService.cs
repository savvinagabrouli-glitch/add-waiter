using Cursework.Application.Interfaces;
using Cursework.Application.Realtime;
using Cursework.Domains.Models;
using Cursework.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Cursework.Application.Services
{
    public class CallWaiterService : ICallWaiterService
    {
        private readonly AppDbContext _db;
        private readonly ICallWaiterNotifications _notifications;

        public CallWaiterService(AppDbContext db, ICallWaiterNotifications notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        public async Task<List<CallWaiter>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.CallWaiters
                .Include(c => c.Table)
                .Include(c => c.Order)
                .OrderBy(c => c.Id)
                .ToListAsync(ct);
        }

        public async Task<CallWaiter> AddAsync(CallWaiter callWaiter, CancellationToken ct = default)
        {
            if (callWaiter.CreatedAt == default)
                callWaiter.CreatedAt = DateTime.UtcNow;

            _db.CallWaiters.Add(callWaiter);
            await _db.SaveChangesAsync(ct);

            await _db.Entry(callWaiter).Reference(c => c.Table).LoadAsync(ct);
            await _db.Entry(callWaiter).Reference(c => c.Order).LoadAsync(ct);

            await _notifications.NotifyCallWaiterChangedAsync(
                    EntityChangeAction.Created,
                    callWaiter,
                    ct);

            return callWaiter;
        }

        public async Task UpdateAsync(CallWaiter callWaiter, CancellationToken ct = default)
        {
            var entity = await _db.CallWaiters
                .FirstOrDefaultAsync(c => c.Id == callWaiter.Id, ct);

            if (entity == null)
                throw new KeyNotFoundException($"Вызов официанта Id={callWaiter.Id} не найден.");

            entity.TableId = callWaiter.TableId;
            entity.OrderId = callWaiter.OrderId;
            entity.Type = callWaiter.Type;
            entity.IsHandled = callWaiter.IsHandled;
            entity.CreatedAt = callWaiter.CreatedAt;
            entity.HandledAt = callWaiter.HandledAt;

            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyCallWaiterChangedAsync(
                    EntityChangeAction.Updated,
                    entity,
                    ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.CallWaiters.FindAsync(new object?[] { id }, ct);
            if (entity == null)
                return;

            _db.CallWaiters.Remove(entity);
            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyCallWaiterChangedAsync(
                    EntityChangeAction.Deleted,
                    entity,
                    ct);
        }
    }
}

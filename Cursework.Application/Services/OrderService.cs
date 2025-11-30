using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Cursework.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Cursework.Application.Realtime;

namespace Cursework.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _db;
        private readonly IOrderNotifications _notifications;

        public OrderService(AppDbContext db, IOrderNotifications notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Include(o => o.Payments)
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
        {
            try
            {
                _db.Orders.Add(order);
                await _db.SaveChangesAsync(ct);

                await _db.Entry(order).Reference(o => o.Table).LoadAsync(ct);
                await _db.Entry(order).Reference(o => o.Waiter).LoadAsync(ct);
                await _db.Entry(order).Collection(o => o.Payments).LoadAsync(ct);

                await _notifications.NotifyOrderChangedAsync(
                        EntityChangeAction.Created,
                        order,
                        ct);

                return order;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UX_Orders_ActivePerTable") == true)
            {
                throw new InvalidOperationException(
                    "Для этого стола уже есть активный заказ. " +
                    "Сначала закройте или отмените текущий заказ, прежде чем создавать новый.");
            }
        }

        public async Task UpdateAsync(Order order, CancellationToken ct = default)
        {
            _db.Orders.Update(order);
            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyOrderChangedAsync(
                    EntityChangeAction.Updated,
                    order,
                    ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Orders.FindAsync(new object?[] { id }, ct);
            if (entity == null)
                return;

            _db.Orders.Remove(entity);
            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyOrderChangedAsync(
                    EntityChangeAction.Deleted,
                    entity,
                    ct);
        }

        public async Task<bool> HasActiveOrderForTableAsync(int tableId, CancellationToken ct = default)
        {
            return await _db.Orders.AnyAsync(o =>
                o.TableId == tableId &&
                (o.Status == "New" || o.Status == "Pending" || o.Status == "ReadyToPay"),
                ct);
        }
    }
}

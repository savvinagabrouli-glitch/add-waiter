using Cursework.Application.Interfaces;
using Cursework.Application.Realtime;
using Cursework.Domains.Models;
using Cursework.Persistence.Db;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cursework.Application.Services
{
    public class TableService : ITableService
    {
        private readonly AppDbContext _db;
        private readonly ITableNotifications _notifications;

        public TableService(AppDbContext db, ITableNotifications notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        public async Task<List<DiningTable>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.Set<DiningTable>()
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .ToListAsync(ct);
        }

        public async Task<DiningTable> AddAsync(DiningTable table, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(table.Name))
                throw new InvalidOperationException("Название стола обязательно.");

            if (table.Seats <= 0)
                throw new InvalidOperationException("Количество мест за столом должно быть больше 0.");

            _db.Set<DiningTable>().Add(table);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql &&
                                               (sql.Number == 2627 || sql.Number == 2601) &&
                                               ex.InnerException.Message.Contains("UQ_DiningTable_Name"))
            {
                // отсоединяем добавленные записи
                foreach (var entry in _db.ChangeTracker
                                 .Entries<DiningTable>()
                                 .Where(e => e.State == EntityState.Added))
                {
                    entry.State = EntityState.Detached;
                }

                throw new InvalidOperationException("Стол с таким названием уже существует.");
            }

            _db.Entry(table).State = EntityState.Detached;

            await _notifications.NotifyTableChangedAsync(
                    EntityChangeAction.Created,
                    table,
                    ct);

            return table;
        }

        public async Task UpdateAsync(DiningTable table, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(table.Name))
                throw new InvalidOperationException("Название стола обязательно.");

            if (table.Seats <= 0)
                throw new InvalidOperationException("Количество мест за столом должно быть больше 0.");

            try
            {
                var existing = await _db.Set<DiningTable>().FindAsync(new object?[] { table.Id }, ct);

                if (existing == null)
                {
                    _db.Attach(table);
                    _db.Entry(table).State = EntityState.Modified;
                }
                else
                {
                    _db.Entry(existing).CurrentValues.SetValues(table);
                }

                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_DiningTable_Name") == true)
            {
                throw new InvalidOperationException("Стол с таким названием уже существует.");
            }

            await _notifications.NotifyTableChangedAsync(
                    EntityChangeAction.Updated,
                    table,
                    ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var hasOrders = await _db.Orders.AnyAsync(o => o.TableId == id);
            var hasCalls = await _db.CallWaiters.AnyAsync(cw => cw.TableId == id);

            if (hasOrders || hasCalls)
                throw new InvalidOperationException(
                    "Нельзя удалить стол, по которому уже есть заказы или вызовы официанта. " +
                    "Вы можете пометить статус стола на 'Не обслуживается'.");

            var entity = await _db.Set<DiningTable>()
                .FindAsync(new object?[] { id }, ct);

            if (entity == null)
                return;

            _db.Remove(entity);
            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyTableChangedAsync(
                    EntityChangeAction.Deleted,
                    entity,
                    ct);
        }
    }
}

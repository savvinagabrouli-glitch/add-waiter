using Cursework.Application.Interfaces;
using Cursework.Application.Realtime;
using Cursework.Domains.Models;
using Cursework.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cursework.Application.Services
{
    public class StaffService : IStaffService
    {
        private readonly AppDbContext _db;
        private readonly IStaffNotifications _notifications;

        public StaffService(AppDbContext db, IStaffNotifications notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        public Task<List<Staff>> GetAllAsync(CancellationToken ct = default) =>
            _db.Set<Staff>().AsNoTracking().ToListAsync(ct);

        public async Task<Staff> AddAsync(Staff staff, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(staff.Name))
                throw new InvalidOperationException("Имя сотрудника обязательно.");

            if (string.IsNullOrWhiteSpace(staff.Login))
                throw new InvalidOperationException("Логин сотрудника обязателен.");

            try
            {
                _db.Add(staff);
                await _db.SaveChangesAsync();

                await _notifications.NotifyStaffChangedAsync(
                        EntityChangeAction.Created,
                        staff,
                        ct);

                return staff;
            }
            catch(DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_Staff_Login") == true)
            {
                throw new InvalidOperationException("Сотрудник с таким логином уже существует.");
            }
        }

        public async Task UpdateAsync(Staff staff, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(staff.Name))
                throw new InvalidOperationException("Имя сотрудника обязательно.");

            if (string.IsNullOrWhiteSpace(staff.Login))
                throw new InvalidOperationException("Логин сотрудника обязателен.");

            try
            {
                _db.Update(staff);
                await _db.SaveChangesAsync();

                await _notifications.NotifyStaffChangedAsync(
                        EntityChangeAction.Updated,
                        staff,
                        ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_Staff_Login") == true)
            {
                throw new InvalidOperationException("Сотрудник с таким логином уже существует.");
            }
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var hasOrders = await _db.Orders.AnyAsync(o => o.WaiterId == id);
            if (hasOrders)
                throw new InvalidOperationException(
                    "Нельзя удалить сотрудника, который уже обслуживал заказы. " +
                    "Вы можете поставить статус сотрудника 'Уволен', или изменить ему логин/пароль.");

            var e = await _db.Set<Staff>().FindAsync(new object?[]  { id }, ct);
            if (e != null)
            {
                _db.Remove(e);
                await _db.SaveChangesAsync();

                await _notifications.NotifyStaffChangedAsync(
                        EntityChangeAction.Deleted,
                        e,
                        ct);
            }
        }
    }
}

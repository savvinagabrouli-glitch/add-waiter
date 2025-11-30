using Cursework.Application.Interfaces;
using Cursework.Application.Models;
using Cursework.Domains.Models;
using Cursework.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cursework.Application.Services
{
    public class OrderDetailsService : IOrderDetailsService
    {
        private readonly AppDbContext _db;

        public OrderDetailsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OrderDetailsDto?> GetAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .Include(o => o.Table)
                .Include(o => o.Guests)
                    .ThenInclude(g => g.OrderItems)
                        .ThenInclude(i => i.Dish)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order == null)
                return null;

            var dto = new OrderDetailsDto
            {
                OrderId = order.Id,
                TableId = order.TableId,
                TableName = order.Table?.Name,
                OrderStatus = order.Status
            };

            foreach (var guest in order.Guests.OrderBy(g => g.Index))
            {
                var gDto = new OrderGuestDto
                {
                    GuestId = guest.Id,
                    Index = guest.Index
                };

                foreach (var item in guest.OrderItems)
                {
                    var unitPrice = item.Quantity > 0
                        ? item.Price / item.Quantity
                        : item.Price;

                    var itemDto = new OrderItemDto
                    {
                        ItemId = item.Id,
                        OrderId = item.OrderId,
                        GuestId = guest.Id,
                        GuestIndex = guest.Index,
                        DishId = item.DishId,
                        DishName = item.Dish?.Name ?? string.Empty,
                        UnitPrice = unitPrice,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Notes = item.Notes,
                        Status = item.Status
                    };

                    gDto.Items.Add(itemDto);
                }

                dto.Guests.Add(gDto);
            }

            var payment = order.Payments
                .OrderByDescending(p => p.PaidAt)
                .FirstOrDefault();

            if (payment != null)
            {
                dto.Payment = new OrderPaymentDto
                {
                    PaymentId = payment.Id,
                    Method = payment.Method,
                    Amount = payment.Amount,
                    PaidAt = payment.PaidAt
                };
            }

            dto.TotalAmount = dto.Guests
                .SelectMany(g => g.Items)
                .Sum(i => i.Price);

            return dto;
        }

        public async Task<OrderDetailsDto> SaveAsync(OrderDetailsDto details, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .Include(o => o.Guests)
                    .ThenInclude(g => g.OrderItems)
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == details.OrderId, ct);

            if (order == null)
                throw new InvalidOperationException($"Заказ #{details.OrderId} не найден.");

            if (order.OrderItems.Any())
                _db.OrderItems.RemoveRange(order.OrderItems);

            if (order.Guests.Any())
                _db.Guests.RemoveRange(order.Guests);

            if (order.Payments.Any())
                _db.Payments.RemoveRange(order.Payments);

            foreach (var gDto in details.Guests.OrderBy(g => g.Index))
            {
                var guest = new Guest
                {
                    OrderId = order.Id,
                    Index = gDto.Index
                };

                order.Guests.Add(guest);

                foreach (var itemDto in gDto.Items)
                {
                    var item = new OrderItem
                    {
                        OrderId = order.Id,
                        DishId = itemDto.DishId,
                        Quantity = itemDto.Quantity,
                        Price = itemDto.Price,
                        Notes = itemDto.Notes,
                        Status = string.IsNullOrWhiteSpace(itemDto.Status)
                            ? "Ordered"
                            : itemDto.Status
                    };

                    item.Guest = guest;
                    order.OrderItems.Add(item);
                }
            }

            if (details.Payment != null && details.Payment.Amount > 0)
            {
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Method = string.IsNullOrWhiteSpace(details.Payment.Method)
                        ? "Cash"
                        : details.Payment.Method,
                    Amount = details.Payment.Amount,
                    PaidAt = details.Payment.PaidAt == default
                        ? DateTime.UtcNow
                        : details.Payment.PaidAt.ToUniversalTime()
                };

                order.Payments.Add(payment);
            }

            await _db.SaveChangesAsync(ct);

            var result = await GetAsync(order.Id, ct);
            if (result == null)
                throw new InvalidOperationException("Не удалось загрузить сохранённые детали заказа.");

            return result;
        }

        public async Task DeleteAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .Include(o => o.Guests)
                    .ThenInclude(g => g.OrderItems)
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order == null)
                return;

            if (order.OrderItems.Any())
                _db.OrderItems.RemoveRange(order.OrderItems);

            if (order.Guests.Any())
                _db.Guests.RemoveRange(order.Guests);

            if (order.Payments.Any())
                _db.Payments.RemoveRange(order.Payments);

            await _db.SaveChangesAsync(ct);
        }

        public async Task<byte[]> GetReceiptPdfAsync(int orderId, CancellationToken ct = default)
        {
            var details = await GetAsync(orderId, ct);
            if (details == null)
                throw new InvalidOperationException($"Заказ #{orderId} не найден, чек сформировать нельзя.");

            var total = details.TotalAmount;
            var payment = details.Payment;

            var paymentMethodDisplay = payment != null
                ? GetPaymentMethodDisplay(payment.Method)
                : "Не указан";

            var cafeName = "Кафе \"Уютный уголок\"";
            var cafeAddress = "г. Димитровград, ул. Куйбышева, 300";
            var cafePhone = "+7 (902) 126-58-35";
            var cafeInn = "ИНН 1234567890";
            var createdAt = DateTime.Now;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A5);

                    page.Content().Column(col =>
                    {
                        col.Item().AlignCenter().Text(cafeName)
                            .FontSize(16).SemiBold();
                        col.Item().AlignCenter().Text(cafeAddress).FontSize(10);
                        col.Item().AlignCenter().Text(cafePhone).FontSize(10);
                        col.Item().AlignCenter().Text(cafeInn).FontSize(10);
                        col.Item().PaddingBottom(5).LineHorizontal(0.5f);

                        col.Item().Text(text =>
                        {
                            text.Span($"Чек по заказу #{details.OrderId}").SemiBold();
                        });
                        col.Item().Text($"Дата: {createdAt:dd.MM.yyyy HH:mm}");
                        col.Item().Text($"Стол: {details.TableName ?? "N/A"}");
                        col.Item().Text($"Гостей: {details.Guests.Count}");
                        col.Item().PaddingBottom(5).LineHorizontal(0.5f);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(0.7f); 
                                columns.RelativeColumn(3f);   
                                columns.RelativeColumn(1f); 
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("#").SemiBold().FontSize(10);
                                header.Cell().Text("Блюдо").SemiBold().FontSize(10);
                                header.Cell().AlignRight().Text("Кол-во").SemiBold().FontSize(10);
                                header.Cell().AlignRight().Text("Цена").SemiBold().FontSize(10);
                                header.Cell().AlignRight().Text("Сумма").SemiBold().FontSize(10);
                            });

                            var allItems = details.Guests
                                .OrderBy(g => g.Index)
                                .SelectMany(g => g.Items)
                                .ToList();

                            int index = 1;
                            foreach (var item in allItems)
                            {
                                table.Cell().Text(index.ToString()).FontSize(10);
                                table.Cell().Text(item.DishName).FontSize(10);
                                table.Cell().AlignRight().Text(item.Quantity.ToString()).FontSize(10);
                                table.Cell().AlignRight().Text($"{item.UnitPrice:0.00}").FontSize(10);
                                table.Cell().AlignRight().Text($"{item.Price:0.00}").FontSize(10);

                                index++;
                            }
                        });

                        col.Item().PaddingTop(5).LineHorizontal(0.5f);

                        col.Item().AlignRight().Text(text =>
                        {
                            text.Span("ИТОГО: ").SemiBold();
                            text.Span($"{total:0.00} ₽").SemiBold();
                        });

                        if (payment != null)
                        {
                            col.Item().Text($"Метод оплаты: {paymentMethodDisplay}");
                            col.Item().Text($"Оплачено: {payment.Amount:0.00} ₽");
                            col.Item().Text($"Дата оплаты: {payment.PaidAt:dd.MM.yyyy HH:mm}");
                        }
                        else
                        {
                            col.Item().Text("Оплата не проведена").FontSize(10);
                        }

                        col.Item().PaddingTop(5).AlignCenter().Text("Спасибо за визит!")
                            .FontSize(10);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string GetPaymentMethodDisplay(string? method)
        {
            return method switch
            {
                "Cash" => "Наличные",
                "Card" => "Карта",
                "QR" => "QR-код",
                "Other" => "Другое",
                _ => method ?? "Не указан"
            };
        }
    }
}

using System;
using System.Collections.Generic;

namespace Cursework.Application.Models
{
    public class OrderDetailsDto
    {
        public int OrderId { get; set; }

        public int TableId { get; set; }

        public string? TableName { get; set; }

        public string? OrderStatus { get; set; }

        public List<OrderGuestDto> Guests { get; set; } = new();

        public OrderPaymentDto? Payment { get; set; }

        public decimal TotalAmount { get; set; }
    }

    public class OrderGuestDto
    {
        public int GuestId { get; set; }

        public int Index { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int ItemId { get; set; }

        public int OrderId { get; set; }

        public int? GuestId { get; set; }

        public int GuestIndex { get; set; }

        public int DishId { get; set; }

        public string DishName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public string? Notes { get; set; }

        public string Status { get; set; } = "Ordered";
    }

    public class OrderPaymentDto
    {
        public int PaymentId { get; set; }

        public string Method { get; set; } = "Cash";

        public decimal Amount { get; set; }

        public DateTime PaidAt { get; set; }
    }
}

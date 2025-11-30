using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace Cursework.Domains.Models;


public partial class Order
{
    public int Id { get; set; }

    public int TableId { get; set; }

    public int? WaiterId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    [ValidateNever]
    public virtual ICollection<CallWaiter> CallWaiters { get; set; } = new List<CallWaiter>();

    [ValidateNever]
    public virtual ICollection<Guest> Guests { get; set; } = new List<Guest>();

    [ValidateNever]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [ValidateNever]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ValidateNever]
    public virtual DiningTable Table { get; set; } = null!;

    [ValidateNever]
    public virtual Staff? Waiter { get; set; }
}

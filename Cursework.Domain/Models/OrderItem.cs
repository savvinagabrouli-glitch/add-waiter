using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursework.Domains.Models;


public partial class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int DishId { get; set; }

    public int? GuestId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = null!;

    public virtual Dish Dish { get; set; } = null!;

    public virtual Guest? Guest { get; set; }

    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;
}

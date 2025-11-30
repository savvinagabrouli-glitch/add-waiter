using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursework.Domains.Models;


public partial class Dish
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }

    [JsonIgnore]
    [ValidateNever]
    public virtual Category Category { get; set; } = null!;

    [JsonIgnore]
    [ValidateNever]
    public virtual ICollection<DishIngredient> DishIngredients { get; set; } = new List<DishIngredient>();

    [JsonIgnore]
    [ValidateNever]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

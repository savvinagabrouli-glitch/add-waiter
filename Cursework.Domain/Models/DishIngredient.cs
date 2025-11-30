using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursework.Domains.Models;


public partial class DishIngredient
{
    public int DishId { get; set; }

    public int IngredientId { get; set; }

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = null!;

    [JsonIgnore]
    [ValidateNever]

    public virtual Dish Dish { get; set; } = null!;

    [JsonIgnore]
    [ValidateNever]
    public virtual Ingredient Ingredient { get; set; } = null!;
}

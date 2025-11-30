using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursework.Domains.Models;


public partial class Ingredient
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int CategoryIngredientId { get; set; }

    [JsonIgnore]
    [ValidateNever]
    public virtual CategoryIngredient CategoryIngredient { get; set; } = null!;

    [JsonIgnore]
    [ValidateNever]
    public virtual ICollection<DishIngredient> DishIngredients { get; set; } = new List<DishIngredient>();
}

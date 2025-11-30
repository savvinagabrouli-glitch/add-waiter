using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursework.Domains.Models;


public partial class Guest
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int Index { get; set; }

    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

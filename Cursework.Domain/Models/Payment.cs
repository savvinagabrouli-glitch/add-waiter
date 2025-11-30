using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursework.Domains.Models;


public partial class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Method { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; }

    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;
}

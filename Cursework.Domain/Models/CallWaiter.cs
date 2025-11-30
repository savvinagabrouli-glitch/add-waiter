using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursework.Domains.Models;
public partial class CallWaiter
{
    public int Id { get; set; }

    public int TableId { get; set; }

    public int? OrderId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? HandledAt { get; set; }

    public string Type { get; set; } = null!;

    public bool IsHandled { get; set; }

    [JsonIgnore]
    [ValidateNever]
    public virtual Order? Order { get; set; }

    [ValidateNever]
    public virtual DiningTable Table { get; set; } = null!;
}

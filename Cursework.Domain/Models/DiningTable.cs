using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursework.Domains.Models;


public partial class DiningTable
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Seats { get; set; }

    public string Status { get; set; } = null!;

    public string Zone { get; set; } = null!;

    [JsonIgnore]
    [ValidateNever]
    public virtual ICollection<CallWaiter> CallWaiters { get; set; } = new List<CallWaiter>();

    [JsonIgnore]
    [ValidateNever]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

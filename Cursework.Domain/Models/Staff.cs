using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cursework.Domains.Models;


public partial class Staff
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

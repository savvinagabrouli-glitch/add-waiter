using Cursework.Domains.Models;

namespace Cursework.Application.Realtime;

public class OrderChangedDto
{
    public EntityChangeAction Action { get; set; }
    public Order Order { get; set; } = null!;
}

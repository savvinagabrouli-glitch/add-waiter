using Cursework.Domains.Models;

namespace Cursework.Application.Realtime;

public class DiningTableChangedDto
{
    public EntityChangeAction Action { get; set; }
    public DiningTable Table { get; set; } = null!;
}

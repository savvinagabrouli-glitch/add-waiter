using Cursework.Domains.Models;

namespace Cursework.Application.Realtime;

public class CallWaiterChangedDto
{
    public EntityChangeAction Action { get; set; }
    public CallWaiter CallWaiter { get; set; } = null!;
}

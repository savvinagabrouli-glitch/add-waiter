using Cursework.Domains.Models;

namespace Cursework.Application.Realtime;

public class StaffChangedDto
{
    public EntityChangeAction Action { get; set; }
    public Staff Staff { get; set; } = null!;
}

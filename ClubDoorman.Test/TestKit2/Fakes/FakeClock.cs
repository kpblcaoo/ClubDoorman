using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public interface IClock
{
    DateTime UtcNow { get; }
    Task Delay(TimeSpan delay, CancellationToken cancellationToken = default);
}

public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; private set; } = DateTime.UtcNow;
    
    public void Advance(TimeSpan delta) => UtcNow += delta;
    public void SetTime(DateTime time) => UtcNow = time;
    
    public Task Delay(TimeSpan delay, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

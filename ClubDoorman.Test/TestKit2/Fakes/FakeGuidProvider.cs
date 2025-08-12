using System;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public interface IGuidProvider
{
    Guid NewGuid();
}

public sealed class DeterministicGuidProvider : IGuidProvider
{
    private int _counter = 0;
    public Guid NewGuid() => new Guid(_counter++, 0, 0, new byte[8]);
}

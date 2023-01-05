using AutoFixture.Xunit2;

namespace Tests;

public class AutoDomainDataAttribute : AutoDataAttribute
{
    public AutoDomainDataAttribute()
        : base(FixtureFactory.CreateFixture)
    {
    }
}

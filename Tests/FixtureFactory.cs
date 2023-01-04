using AutoFixture;
using AutoFixture.AutoMoq;

namespace Tests;

public static class FixtureFactory
{
    public static IFixture CreateFixture()
    {
        var fixture = new Fixture();

        fixture.Customize(new CompositeCustomization(
            new AutoMoqCustomization()
        ));

        return fixture;
    }
}

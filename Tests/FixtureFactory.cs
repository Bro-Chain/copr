using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;

namespace Tests;

public static class FixtureFactory
{
    public static IFixture CreateFixture()
    {
        var fixture = new Fixture();

        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        fixture.Customize(new CompositeCustomization(
            new AutoMoqCustomization()
        ));

        return fixture;
    }
}

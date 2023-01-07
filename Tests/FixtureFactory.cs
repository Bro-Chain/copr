using System.Linq;
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

        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        return fixture;
    }
}

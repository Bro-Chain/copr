using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using CosmosProposalBot.Data;
using CosmosProposalBot.Util;
using Discord;
using Moq;
using AutoFixture.Xunit2;
using Xunit;

namespace Tests.Util;

public static class FixtureFactory
{
    public static IFixture CreateFixture()
    {
        var fixture = new Fixture();

        // The order matters here. As soon as a customization handles a type, AutoFixture
        // will stop. AutoMoqCustomization handles a lot of types so it should be last.
        fixture.Customize(new CompositeCustomization(
            new AutoMoqCustomization()
        ));

        return fixture;
    }
}
public class AutoDomainDataAttribute : AutoDataAttribute
{
    public AutoDomainDataAttribute()
        : base(FixtureFactory.CreateFixture)
    {
    }
}

public class PermissionHelperTests
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly Mock<IInteractionContext> _interactionContextMock;
    private readonly Mock<CopsDbContext> _dbContextMock;
    private readonly Mock<IGuildUser> _guildUserMock;
    private readonly PermissionHelper _permissionHelper;

    public PermissionHelperTests( )
    {
        _guildUserMock = _fixture.Freeze<Mock<IGuildUser>>();
        _interactionContextMock = _fixture.Freeze<Mock<IInteractionContext>>();
        _interactionContextMock
            .Setup( m => m.User )
            .Returns( _guildUserMock.Object );
        
        _dbContextMock = _fixture.Freeze<Mock<CopsDbContext>>();

        _permissionHelper = _fixture.Create<PermissionHelper>();
    }
    
    [Theory]
    [AutoDomainData]
    public async Task EnsureUserHasPermission()
    {
        await _permissionHelper.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object );
        
    }
}

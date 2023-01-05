using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Data;
using CosmosProposalBot.Util;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Tests.Util;

public class SubscriptionHelperTest
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly SubscriptionHelper _subscriptionHelper;
    private readonly Mock<IPermissionHelper> _permissionHelperMock;
    private readonly Mock<CopsDbContext> _dbContextMock;
    private readonly Mock<IInteractionContext> _interactionContextMock;
    private readonly ServiceCollection _services;
    private readonly Mock<IDiscordInteraction> _discordInteractionMock;

    public SubscriptionHelperTest( )
    {
        _permissionHelperMock = _fixture.Freeze<Mock<IPermissionHelper>>();

        _discordInteractionMock = _fixture.Freeze<Mock<IDiscordInteraction>>();
        _interactionContextMock = _fixture.Freeze<Mock<IInteractionContext>>();
        _interactionContextMock.Setup( m => m.Interaction )
            .Returns( _discordInteractionMock.Object );
        _dbContextMock = _fixture.Freeze<Mock<CopsDbContext>>();

        _services = new ServiceCollection();
        _fixture.Inject(_services);
        _services.AddSingleton(_permissionHelperMock.Object);
        _services.AddSingleton(_dbContextMock.Object);
        _services.AddSingleton(_interactionContextMock.Object);
        _fixture.Inject( _services.BuildServiceProvider() as IServiceProvider );
        
        _subscriptionHelper = _fixture.Create<SubscriptionHelper>();
    }
    
    [Theory]
    [AutoDomainData]
    public async Task SubscribeChannel_BadPermissions( string chainName )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( It.IsAny<IInteractionContext>(), It.IsAny<CopsDbContext>() ) )
            .ReturnsAsync( false );
        
        await _subscriptionHelper.SubscribeChannel( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true,null,null,null,null ) );
        
    }
}

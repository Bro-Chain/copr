using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Data;
using CosmosProposalBot.Util;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Tests.Util;

public class ConfigModuleActionsTest
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly ConfigModuleActions _configModuleActions;
    private readonly Mock<IInteractionContext> _interactionContextMock;
    private readonly Mock<IDiscordInteraction> _interactionMock;
    private readonly Mock<CopsDbContext> _dbContextMock;
    private ServiceCollection _services;
    private readonly Mock<IPermissionHelper> _permissionHelperMock;

    public ConfigModuleActionsTest( )
    {
        _interactionMock = _fixture.Freeze<Mock<IDiscordInteraction>>();
        _interactionContextMock = _fixture.Freeze<Mock<IInteractionContext>>();
        _interactionContextMock.Setup( m => m.Interaction )
            .Returns( _interactionMock.Object );

        _permissionHelperMock = _fixture.Freeze<Mock<IPermissionHelper>>();
        
        _dbContextMock = _fixture.Freeze<Mock<CopsDbContext>>();
        _dbContextMock.Setup( m => m.SaveChangesAsync( default ) )
            .ReturnsAsync( 0 );
        _dbContextMock.Setup( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ) )
            .ReturnsAsync( 0 );

        _services = new ServiceCollection();
        _fixture.Inject( _services );
        _services.AddSingleton( _permissionHelperMock.Object );
        // _services.AddSingleton( _socketClientMock.Object );
        _services.AddSingleton( _dbContextMock.Object );
        _fixture.Inject( _services.BuildServiceProvider() as IServiceProvider );

        _configModuleActions = _fixture.Create<ConfigModuleActions>();
    }
    
    [Fact]
    public async Task AllowRole_PermissionDenied()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( false );
        
        await _configModuleActions.AllowRoleAsync( _interactionContextMock.Object, default(IRole) );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
        
        _dbContextMock.Verify( m => m.Guilds, Times.Never );
    }
    
    [Fact]
    public async Task RevokeRole_PermissionDenied()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( false );
        
        await _configModuleActions.RevokeRoleAsync( _interactionContextMock.Object, default(IRole) );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
        
        _dbContextMock.Verify( m => m.Guilds, Times.Never );
    }
    
    [Fact]
    public async Task ListRoles_PermissionDenied()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( false );
        
        await _configModuleActions.ListRolesAsync( _interactionContextMock.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
        
        _dbContextMock.Verify( m => m.Guilds, Times.Never );
    }
    
    [Fact]
    public async Task AddEndpoint_PermissionDenied()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( false );
        
        await _configModuleActions.AddEndpointAsync( _interactionContextMock.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        _interactionMock.Verify( m => m.RespondWithModalAsync( It.IsAny<Modal>(), null), Times.Never );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
    }
    
    [Fact]
    public async Task RemoveEndpoint_PermissionDenied()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( false );
        
        await _configModuleActions.RemoveEndpointAsync( _interactionContextMock.Object, It.IsAny<string>(), It.IsAny<string>() );

        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null,null ), Times.Once );
        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
        
        _dbContextMock.Verify( m => m.Chains, Times.Never );
    }
    
    [Fact]
    public async Task AddCustomChain_PermissionDenied()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( false );
        
        await _configModuleActions.AddCustomChainAsync( _interactionContextMock.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        _interactionMock.Verify( m => m.RespondWithModalAsync( It.IsAny<Modal>(), null), Times.Never );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
    }
    
    [Fact]
    public async Task RemoveCustomChain_PermissionDenied()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( false );
        
        await _configModuleActions.RemoveCustomChainAsync( _interactionContextMock.Object, It.IsAny<string>() );

        _interactionMock.Verify( m => m.DeferAsync( false, null ), Times.Once );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
        
        _dbContextMock.Verify( m => m.Chains, Times.Never );
    }
}

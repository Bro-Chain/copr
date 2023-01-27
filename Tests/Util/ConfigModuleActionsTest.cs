using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Util;
using Discord;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.EntityFrameworkCore;
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
    private readonly Mock<IGuild> _guildMock;
    
    private readonly ulong _adminRoleId = 10203L;
    private readonly ulong _guildId = 10203L;
    private readonly Mock<IRole> _guildRole;

    public ConfigModuleActionsTest( )
    {
        _guildRole = _fixture.Freeze<Mock<IRole>>();
        _guildRole.Setup( r => r.Id )
            .Returns( _adminRoleId );
        _guildRole.Setup( r => r.Name )
            .Returns( "SuperAdmin" );
        
        _guildMock = _fixture.Freeze<Mock<IGuild>>();
        _guildMock.Setup( m => m.Id )
            .Returns( _guildId );
        _guildMock.Setup( m => m.Roles )
            .Returns( new List<IRole> { _guildRole.Object } );
        
        _interactionMock = _fixture.Freeze<Mock<IDiscordInteraction>>();
        _interactionContextMock = _fixture.Freeze<Mock<IInteractionContext>>();
        _interactionContextMock.Setup( m => m.Interaction )
            .Returns( _interactionMock.Object );
        _interactionContextMock.Setup( m => m.Guild )
            .Returns( _guildMock.Object );

        _permissionHelperMock = _fixture.Freeze<Mock<IPermissionHelper>>();

        var guilds = new List<Guild>()
        {
            new() // guild with role and user
            {
                GuildId = _guildId,
                AdminRoles = new ()
                {
                    new()
                    {
                        RoleId = _adminRoleId
                    }
                },
            }
        };
        
        _dbContextMock = _fixture.Freeze<Mock<CopsDbContext>>();
        _dbContextMock.Setup( m => m.Remove( It.IsAny<Chain?>() ) )
            .Returns<Chain>( ( entity ) => default );
        _dbContextMock.Setup( m => m.SaveChangesAsync( default ) )
            .ReturnsAsync( 0 );
        _dbContextMock.Setup( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ) )
            .ReturnsAsync( 0 );
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( guilds );
        _dbContextMock.Setup( m => m.Endpoints )
            .ReturnsDbSet( new List<Endpoint>() );

        _services = new ServiceCollection();
        _fixture.Inject( _services );
        _services.AddSingleton( _permissionHelperMock.Object );
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
    public async Task AllowRole_NullRole_ShouldThrow()
    {
        var emptyGuildList = new List<Guild>();
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( emptyGuildList );
        
        await _configModuleActions.AllowRoleAsync( _interactionContextMock.Object, default(IRole) );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
        
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        emptyGuildList.Count.Should().Be( 0 );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
        
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
    }
    
    [Fact]
    public async Task AllowRole_AddNewGuild()
    {
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "permission" );
                text.Should().Contain( "added" );
                text.Should().NotContain( "already" );
            } );
        
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( new List<Guild>() );

        var role = new Mock<IRole>();
        role.Setup( m => m.Guild )
            .Returns( _guildMock.Object );
        
        await _configModuleActions.AllowRoleAsync( _interactionContextMock.Object, role.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
        
        _dbContextMock.Verify( m => m.Guilds, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
        _interactionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Never );
    }
    
    [Fact]
    public async Task AllowRole_ExistingGuildNewRole()
    {
        var guilds = new List<Guild>()
        {
            new() // guild with role and user
            {
                GuildId = _guildId
            }
        };
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( guilds );
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "permission" );
                text.Should().Contain( "added" );
                text.Should().NotContain( "already" );
            } );
        
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );

        var role = new Mock<IRole>();
        role.Setup( m => m.Guild )
            .Returns( _guildMock.Object );
        
        await _configModuleActions.AllowRoleAsync( _interactionContextMock.Object, role.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
        
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
        _interactionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Never );
    }
    
    [Fact]
    public async Task AllowRole_ExistingGuildExisingRole()
    {
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "permission" );
                text.Should().NotContain( "added" );
                text.Should().Contain( "already" );
            } );
        
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );

        var role = new Mock<IRole>();
        role.Setup( m => m.Guild )
            .Returns( _guildMock.Object );
        role.Setup( m => m.Id )
            .Returns( _adminRoleId );
        
        await _configModuleActions.AllowRoleAsync( _interactionContextMock.Object, role.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _permissionHelperMock.Verify( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object), Times.Once);
        
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
        _interactionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Never );
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
    public async Task RevokeRole_NullRole_ShouldThrow()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        await _configModuleActions.RevokeRoleAsync( _interactionContextMock.Object, default(IRole) );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
        
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
    }
    
    [Fact]
    public async Task RevokeRole_MissingGuild()
    {
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( new List<Guild>() );
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "permission" );
                text.Should().NotContain( "revoked" );
                text.Should().Contain( "is not" );
            } );
        
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );

        var role = new Mock<IRole>();
        role.Setup( m => m.Guild )
            .Returns( _guildMock.Object );
        
        await _configModuleActions.RevokeRoleAsync( _interactionContextMock.Object, role.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        _interactionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );

        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Fact]
    public async Task RevokeRole_ExistingGuildNewRole()
    {
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "permission" );
                text.Should().NotContain( "revoked" );
                text.Should().Contain( "is not" );
            } );
        
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );

        var role = new Mock<IRole>();
        role.Setup( m => m.Guild )
            .Returns( _guildMock.Object );
        
        await _configModuleActions.RevokeRoleAsync( _interactionContextMock.Object, role.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        _interactionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Never );

        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Fact]
    public async Task RevokeRole_RoleRevoked()
    {
        var guilds = new List<Guild>()
        {
            new()
            {
                GuildId = _guildId,
                AdminRoles = new()
                {
                    new()
                    {
                        RoleId = _adminRoleId
                    }
                }
            }
        };
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( guilds );
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "permission" );
                text.Should().Contain( "revoked" );
                text.Should().NotContain( "is not" );
            } );
        
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );

        var role = new Mock<IRole>();
        role.Setup( m => m.Guild )
            .Returns( _guildMock.Object );
        role.Setup( m => m.Id )
            .Returns( _adminRoleId );
        
        await _configModuleActions.RevokeRoleAsync( _interactionContextMock.Object, role.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        _interactionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Never );

        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
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
    public async Task ListRoles_MissingGuild()
    {
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( new List<Guild>() );
        
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "not have permission" );
                text.Should().Contain( "No roles" );
                text.Should().NotContain( "The following" );
            } );
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        await _configModuleActions.ListRolesAsync( _interactionContextMock.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        _interactionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Never );
    }
    
    [Fact]
    public async Task ListRoles_ExistingGuildMissingAdminRoles()
    {
        var guilds = new List<Guild>()
        {
            new()
            {
                GuildId = _guildId,
            }
        };
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( guilds );
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "not have permission" );
                text.Should().Contain( "No roles" );
                text.Should().NotContain( "The following" );
            } );
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        await _configModuleActions.ListRolesAsync( _interactionContextMock.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        _interactionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Never );
    }
    
    [Fact]
    public async Task ListRoles_Success()
    {
        var guilds = new List<Guild>()
        {
            new()
            {
                GuildId = _guildId,
                AdminRoles = new()
                {
                    new()
                    {
                        RoleId = _adminRoleId
                    }
                }
            }
        };
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( guilds );
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "not have permission" );
                text.Should().NotContain( "No roles" );
                text.Should().Contain( "The following" );
            } );
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        await _configModuleActions.ListRolesAsync( _interactionContextMock.Object );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Once );
        _dbContextMock.Verify( m => m.Guilds, Times.Once );
        _interactionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Once );
        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null, null ), Times.Never );
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
    public async Task AddEndpoint_ModalCreated()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        await _configModuleActions.AddEndpointAsync( _interactionContextMock.Object );

        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null,null ), Times.Never );
        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        _interactionMock.Verify( m => m.RespondWithModalAsync( It.IsAny<Modal>(), null ), Times.Once );
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
    public async Task RemoveEndpoint_ChainMissing()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "not have permission" );
                text.Should().Contain( "no chain registered" );
                text.Should().NotContain( "no endpoint registered" );
                text.Should().NotContain( "Endpoint for provider" );
            } );

        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet( new List<Chain>() );
        
        await _configModuleActions.RemoveEndpointAsync( _interactionContextMock.Object, It.IsAny<string>(), It.IsAny<string>() );

        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null,null ), Times.Once );
        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.Endpoints, Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task RemoveEndpoint_ExistingChainMissingProvider( string chainName )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "not have permission" );
                text.Should().NotContain( "no chain registered" );
                text.Should().Contain( "no endpoint registered" );
                text.Should().NotContain( "Endpoint for provider" );
            } );

        var chains = new List<Chain>()
        {
            new ()
            {
                Name = chainName,
                Endpoints = new List<Endpoint>
                { }
            }
        };
        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet( chains );
        
        await _configModuleActions.RemoveEndpointAsync( _interactionContextMock.Object, chainName, It.IsAny<string>() );

        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null,null ), Times.Once );
        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.Endpoints, Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task RemoveEndpoint_Success( string chainName, string providerName )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "not have permission" );
                text.Should().NotContain( "no chain registered" );
                text.Should().NotContain( "no endpoint registered" );
                text.Should().Contain( "Endpoint for provider" );
            } );

        var chains = new List<Chain>()
        {
            new ()
            {
                Name = chainName,
                Endpoints = new List<Endpoint>
                {
                    new()
                    {
                        Provider = providerName
                    }
                }
            }
        };

        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet( chains );
        
        await _configModuleActions.RemoveEndpointAsync( _interactionContextMock.Object, chainName, providerName );

        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null,null ), Times.Once );
        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.Endpoints, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
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
    public async Task AddCustomChain_ModalCreated()
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        await _configModuleActions.AddCustomChainAsync( _interactionContextMock.Object );

        _interactionMock.Verify( m => m.RespondAsync( It.IsAny<string>(), null, false, true, null, null, null,null ), Times.Never );
        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        _interactionMock.Verify( m => m.RespondWithModalAsync( It.IsAny<Modal>(), null ), Times.Once );
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
    
    [Theory]
    [AutoDomainData]
    public async Task RemoveCustomChain_ChainMissing( string chainName )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "not have permission" );
                text.Should().Contain( $"No custom chain by name '{chainName}'" );
                text.Should().NotContain( "Successfully removed tracking" );
            } );

        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet( new List<Chain>() );
        
        await _configModuleActions.RemoveCustomChainAsync( _interactionContextMock.Object, chainName );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task RemoveCustomChain_ExistingChainWrongGuild( string chainName, ulong chainGuildId, ulong userGuildId )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "not have permission" );
                text.Should().Contain( $"No custom chain by name '{chainName}'" );
                text.Should().NotContain( "Successfully removed tracking" );
            } );

        var guildMock = new Mock<IGuild>();
        guildMock.Setup( g => g.Id )
            .Returns( userGuildId );

        _interactionContextMock.Setup( m => m.Guild )
            .Returns( guildMock.Object );

        var chains = new List<Chain>()
        {
            new ()
            {
                Name = chainName,
                CustomForGuildId = chainGuildId
            }
        };
        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet( chains );
        
        await _configModuleActions.RemoveCustomChainAsync( _interactionContextMock.Object, chainName );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task RemoveCustomChain_ExistingChainCorrectGuild( string chainName, ulong chainGuildId )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object ) )
            .ReturnsAsync( true );
        
        _interactionMock.Setup( m => m.FollowupAsync(  It.IsAny<string>(), null, false, true, null, null, null, null ))
            .Callback( ( string text, Embed[] embeds, bool isTTS, bool ephemeral, AllowedMentions allowedMentions, MessageComponent components, Embed embed, RequestOptions options ) =>
            {
                text.Should().NotContain( "not have permission" );
                text.Should().NotContain( $"No custom chain by name '{chainName}'" );
                text.Should().Contain( "Successfully removed tracking" );
            } );

        var guildMock = new Mock<IGuild>();
        guildMock.Setup( g => g.Id )
            .Returns( chainGuildId );

        _interactionContextMock.Setup( m => m.Guild )
            .Returns( guildMock.Object );

        var chains = new List<Chain>()
        {
            new ()
            {
                Name = chainName,
                CustomForGuildId = chainGuildId
            }
        };
        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet( chains );
        
        await _configModuleActions.RemoveCustomChainAsync( _interactionContextMock.Object, chainName );

        _interactionMock.Verify( m => m.DeferAsync( true, null ), Times.Never );
        
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
    }
}

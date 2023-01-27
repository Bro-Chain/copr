using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Util;
using Discord;
using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Tests.Util;

public class PermissionHelperTest
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly Mock<IInteractionContext> _interactionContextMock;
    private readonly Mock<CopsDbContext> _dbContextMock;
    private readonly Mock<IGuildUser> _guildUserMock;
    private readonly PermissionHelper _permissionHelper;
    private readonly Mock<IGuild> _guildMock;
    
    private readonly ulong _adminRoleId = 10203L;
    private readonly ulong _adminUserId = 20304L;
    private readonly ulong _guildWithUserAndRole = 40506L;
    private readonly ulong _guildWithoutUserOrRole = 50607L;

    public PermissionHelperTest()
    {
        _guildMock = _fixture.Freeze<Mock<IGuild>>();
        _guildUserMock = _fixture.Freeze<Mock<IGuildUser>>();
        _guildUserMock.Setup( m => m.Guild )
            .Returns( _guildMock.Object );
        _interactionContextMock = _fixture.Freeze<Mock<IInteractionContext>>();
        _interactionContextMock
            .Setup( m => m.User )
            .Returns( _guildUserMock.Object );
        _interactionContextMock
            .Setup( m => m.Guild )
            .Returns( _guildMock.Object );

        var guilds = new List<Guild>()
        {
            new() // guild with role and user
            {
                GuildId = _guildWithUserAndRole,
                AdminRoles = new ()
                {
                    new()
                    {
                        RoleId = _adminRoleId
                    }
                },
                AdminUsers = new ()
                {
                    new ()
                    {
                        UserId = _adminUserId
                    }
                }
            },
            new () // guild without role or user
            {
                GuildId = _guildWithoutUserOrRole
            },
        };
        _dbContextMock = _fixture.Freeze<Mock<CopsDbContext>>();
        _dbContextMock.Setup( m => m.Guilds )
            .ReturnsDbSet( guilds );

        _permissionHelper = _fixture.Create<PermissionHelper>();
    }
    
    [Fact]
    public async Task EnsureUserHasPermission_AdminUserAndRole()
    {
        _guildUserMock.Setup( m=> m.RoleIds)
            .Returns( () => new List<ulong>() { _adminRoleId } );
        _guildMock.Setup( m => m.Id )
            .Returns( _guildWithUserAndRole );

        var result = await _permissionHelper.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object );
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task EnsureUserHasPermission_AdminUserAccess()
    {
        _guildUserMock.Setup( m=> m.RoleIds)
            .Returns( () => new List<ulong>() {  } );
        _guildMock.Setup( m => m.Id )
            .Returns( _guildWithUserAndRole );

        var result = await _permissionHelper.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object );
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task EnsureUserHasPermission_AdminRoleAccess()
    {
        _guildUserMock.Setup( m=> m.RoleIds)
            .Returns( () => new List<ulong>() { _adminRoleId } );
        _guildMock.Setup( m => m.Id )
            .Returns( _guildWithUserAndRole );

        var result = await _permissionHelper.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object );
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task EnsureUserHasPermission_NoAdminRolesOrUsersDefined()
    {
        _guildUserMock.Setup( m=> m.RoleIds)
            .Returns( () => new List<ulong>() {} );
        _guildMock.Setup( m => m.Id )
            .Returns( _guildWithoutUserOrRole );

        var result = await _permissionHelper.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object );
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task EnsureUserHasPermission_GuildNotFoundInDatabase()
    {
        _guildUserMock.Setup( m=> m.RoleIds)
            .Returns( () => new List<ulong>() { _adminRoleId } );
        _guildMock.Setup( m => m.Id )
            .Returns( 1 );

        var result = await _permissionHelper.EnsureUserHasPermission( _interactionContextMock.Object, _dbContextMock.Object );
        result.Should().BeTrue();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Util;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
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
    private readonly Mock<IMessageChannel> _messageChannelMock;
    private readonly Mock<IGuild> _guildMock;
    private readonly Mock<IUser> _userMock;

    public SubscriptionHelperTest()
    {
        _permissionHelperMock = _fixture.Freeze<Mock<IPermissionHelper>>();

        _discordInteractionMock = _fixture.Freeze<Mock<IDiscordInteraction>>();
        _guildMock = _fixture.Freeze<Mock<IGuild>>();
        _messageChannelMock = _fixture.Freeze<Mock<IMessageChannel>>();
        _userMock = _fixture.Freeze<Mock<IUser>>();
        _interactionContextMock = _fixture.Freeze<Mock<IInteractionContext>>();
        _interactionContextMock.Setup( m => m.Interaction )
            .Returns( _discordInteractionMock.Object );
        _interactionContextMock.Setup( m => m.Guild )
            .Returns( _guildMock.Object );
        _interactionContextMock.Setup( m => m.Channel )
            .Returns( _messageChannelMock.Object );
        _interactionContextMock.Setup( m => m.User )
            .Returns( _userMock.Object );

        _dbContextMock = _fixture.Freeze<Mock<CopsDbContext>>();
        _dbContextMock.Setup( m => m.SaveChangesAsync(default ) )
            .ReturnsAsync( 0 );

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

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ) );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task SubscribeChannel_ExistingSubscriptionStandardChain( string chainName, ulong channelId )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( It.IsAny<IInteractionContext>(), It.IsAny<CopsDbContext>() ) )
            .ReturnsAsync( true );
        _messageChannelMock.Setup( m => m.Id )
            .Returns( channelId );
        
        var chain = new Chain()
        {
            Name = chainName,
        };
        var channelSubscriptions = new List<ChannelSubscription>
        {
            new ()
            {
                Chain = chain,
                DiscordChannelId = channelId
            }
        };
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .Returns( channelSubscriptions.AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.SubscribeChannel( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ) );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.Chains, Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task SubscribeChannel_ExistingSubscriptionCustomChain( string chainName, ulong channelId, ulong guildId )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( It.IsAny<IInteractionContext>(), It.IsAny<CopsDbContext>() ) )
            .ReturnsAsync( true );
        _messageChannelMock.Setup( m => m.Id )
            .Returns( channelId );
        _guildMock.Setup( m => m.Id )
            .Returns( guildId );
        
        var chain = new Chain()
        {
            Name = chainName,
        };
        var channelSubscriptions = new List<ChannelSubscription>
        {
            new ()
            {
                Chain = chain,
                DiscordChannelId = channelId,
                GuildId = guildId
            }
        };
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .Returns( channelSubscriptions.AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.SubscribeChannel( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ) );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.Chains, Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task SubscribeChannel_ChainNotFound( string chainName, ulong channelId )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( It.IsAny<IInteractionContext>(), It.IsAny<CopsDbContext>() ) )
            .ReturnsAsync( true );
        _messageChannelMock.Setup( m => m.Id )
            .Returns( channelId );

        _dbContextMock.Setup( m => m.Chains )
            .Returns( new List<Chain>().AsQueryable().BuildMockDbSet().Object );
        
        var emptyDbSet = new List<ChannelSubscription>().AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .Returns( emptyDbSet.Object );
        
        await _subscriptionHelper.SubscribeChannel( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ) );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        emptyDbSet.Verify( m => m.Add( It.IsAny<ChannelSubscription>()), Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task SubscribeChannel_SubscriptionAdded( string chainName, ulong channelId )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( It.IsAny<IInteractionContext>(), It.IsAny<CopsDbContext>() ) )
            .ReturnsAsync( true );
        _messageChannelMock.Setup( m => m.Id )
            .Returns( channelId );

        var chains = new List<Chain>()
        {
            new ()
            {
                Name = chainName,
            }
        };

        _dbContextMock.Setup( m => m.Chains )
            .Returns( chains.AsQueryable().BuildMockDbSet().Object );
        
        var emptyDbSet = new List<ChannelSubscription>().AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .Returns( emptyDbSet.Object );
        
        await _subscriptionHelper.SubscribeChannel( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ) );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
        emptyDbSet.Verify( m => m.Add( It.IsAny<ChannelSubscription>()), Times.Once );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task UnsubscribeChannel_NoSubscription( string chainName, ulong channelId )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( It.IsAny<IInteractionContext>(), It.IsAny<CopsDbContext>() ) )
            .ReturnsAsync( true );
        _messageChannelMock.Setup( m => m.Id )
            .Returns( channelId );
        
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .Returns( new List<ChannelSubscription>().AsQueryable().BuildMockDbSet().Object );
        _dbContextMock.Setup( m => m.SaveChangesAsync(default ) )
            .ReturnsAsync( 0 );
        
        await _subscriptionHelper.UnsubscribeChannel( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ) );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task UnsubscribeChannel_SubscriptionRemovedStandardChain( string chainName, ulong channelId )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( It.IsAny<IInteractionContext>(), It.IsAny<CopsDbContext>() ) )
            .ReturnsAsync( true );
        _messageChannelMock.Setup( m => m.Id )
            .Returns( channelId );
        
        var chain = new Chain()
        {
            Name = chainName,
        };
        var channelSubscriptions = new List<ChannelSubscription>
        {
            new ()
            {
                Chain = chain,
                DiscordChannelId = channelId,
            }
        };
        
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .Returns( channelSubscriptions.AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.UnsubscribeChannel( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ) );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task UnsubscribeChannel_SubscriptionRemovedCustomChain( string chainName, ulong channelId, ulong guildId )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( It.IsAny<IInteractionContext>(), It.IsAny<CopsDbContext>() ) )
            .ReturnsAsync( true );
        _messageChannelMock.Setup( m => m.Id )
            .Returns( channelId );
        _guildMock.Setup( m => m.Id )
            .Returns( guildId );
        
        var chain = new Chain()
        {
            Name = chainName,
        };
        var channelSubscriptions = new List<ChannelSubscription>
        {
            new ()
            {
                Chain = chain,
                DiscordChannelId = channelId,
                GuildId = guildId
            }
        };
        
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .Returns( channelSubscriptions.AsQueryable().BuildMockDbSet().Object );
        _dbContextMock.Setup( m => m.SaveChangesAsync(default ) )
            .ReturnsAsync( 0 );
        
        await _subscriptionHelper.UnsubscribeChannel( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ) );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task UnsubscribeChannel_BadPermissions( string chainName )
    {
        _permissionHelperMock.Setup( m => m.EnsureUserHasPermission( It.IsAny<IInteractionContext>(), It.IsAny<CopsDbContext>() ) )
            .ReturnsAsync( false );
        
        await _subscriptionHelper.UnsubscribeChannel( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ) );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task SubscribeDm_ExistingSubscriptionStandardChain( string chainName, ulong userId )
    {
        _userMock.Setup( m => m.Id )
            .Returns( userId );
        
        var chain = new Chain()
        {
            Name = chainName,
        };
        var userSubscriptions = new List<UserSubscription>
        {
            new ()
            {
                Chain = chain,
                DiscordUserId = userId
            }
        };
        _dbContextMock.Setup( m => m.UserSubscriptions )
            .Returns( userSubscriptions.AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.SubscribeDm( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ), Times.Once );
        _dbContextMock.Verify( m => m.UserSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.Chains, Times.Never );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task SubscribeDm_ExistingSubscriptionCustomChain( string chainName, ulong userId, ulong guildId )
    {
        _userMock.Setup( m => m.Id )
            .Returns( userId );
        var chains = new List<Chain>()
        {
            new ()
            {
                Name = chainName,
                CustomForGuildId = guildId
            }
        };
        var userSubscriptions = new List<UserSubscription>
        {
            new ()
            {
                Chain = chains.First(),
                DiscordUserId = userId
            }
        };
        _dbContextMock.Setup( m => m.Chains )
            .Returns( chains.AsQueryable().BuildMockDbSet().Object );
        _dbContextMock.Setup( m => m.UserSubscriptions )
            .Returns( userSubscriptions.AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.SubscribeDm( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ), Times.Once );
        _dbContextMock.Verify( m => m.UserSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task SubscribeDm_ChainNotFound( string chainName, ulong userId )
    {
        _userMock.Setup( m => m.Id )
            .Returns( userId );
        
        var chain = new Chain()
        {
            Name = chainName,
        };
        var userSubscriptions = new List<UserSubscription>
        {
            new ()
            {
                Chain = chain,
                DiscordUserId = userId
            }
        };

        _dbContextMock.Setup( m => m.Chains )
            .Returns( new List<Chain>().AsQueryable().BuildMockDbSet().Object );
        _dbContextMock.Setup( m => m.UserSubscriptions )
            .Returns( userSubscriptions.AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.SubscribeDm( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ), Times.Once );
        _dbContextMock.Verify( m => m.UserSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.Chains, Times.Never );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task SubscribeDm_SubscriptionAdded( string chainName, ulong userId )
    {
        _userMock.Setup( m => m.Id )
            .Returns( userId );
        
        var chains = new List<Chain>()
        {
            new ()
            {
                Name = chainName
            }
        };
        _dbContextMock.Setup( m => m.Chains )
            .Returns( chains.AsQueryable().BuildMockDbSet().Object );
        _dbContextMock.Setup( m => m.UserSubscriptions )
            .Returns( new List<UserSubscription>().AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.SubscribeDm( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ), Times.Once );
        _dbContextMock.Verify( m => m.UserSubscriptions, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task UnsubscribeDm_NoSubscription( string chainName, ulong userId )
    {
        _userMock.Setup( m => m.Id )
            .Returns( userId );
        
        _dbContextMock.Setup( m => m.UserSubscriptions )
            .Returns( new List<UserSubscription>().AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.UnsubscribeDm( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ), Times.Once );
        _dbContextMock.Verify( m => m.UserSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.Chains, Times.Never );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task UnsubscribeDm_ChainNotFound( string chainName, ulong userId )
    {
        _userMock.Setup( m => m.Id )
            .Returns( userId );
        
        var chain = new Chain()
        {
            Name = chainName,
        };
        var userSubscriptions = new List<UserSubscription>
        {
            new ()
            {
                Chain = chain,
                DiscordUserId = userId
            }
        };
        _dbContextMock.Setup( m => m.Chains )
            .Returns( new List<Chain>().AsQueryable().BuildMockDbSet().Object );
        _dbContextMock.Setup( m => m.UserSubscriptions )
            .Returns( userSubscriptions.AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.UnsubscribeDm( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ), Times.Once );
        _dbContextMock.Verify( m => m.UserSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Never );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task UnsubscribeDm_SubscriptionRemoved( string chainName, ulong userId )
    {
        _userMock.Setup( m => m.Id )
            .Returns( userId );
        
        var chains = new List<Chain>()
        {
            new ()
            {
                Name = chainName
            }
        };
        var userSubscriptions = new List<UserSubscription>
        {
            new ()
            {
                Chain = chains.First(),
                DiscordUserId = userId
            }
        };
        _dbContextMock.Setup( m => m.Chains )
            .Returns( chains.AsQueryable().BuildMockDbSet().Object );
        _dbContextMock.Setup( m => m.UserSubscriptions )
            .Returns( userSubscriptions.AsQueryable().BuildMockDbSet().Object );
        
        await _subscriptionHelper.UnsubscribeDm( _interactionContextMock.Object, chainName );

        _discordInteractionMock.Verify( m => m.FollowupAsync( It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<bool>(),null,null,null,null ), Times.Once );
        _dbContextMock.Verify( m => m.UserSubscriptions, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.Chains, Times.Once );
        _dbContextMock.Verify( m => m.SaveChangesAsync( default ), Times.Once );
    }
}

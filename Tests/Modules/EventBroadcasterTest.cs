using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using CosmosProposalBot.Modules;
using CosmosProposalBot.Util;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MockQueryable.Moq;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Tests.Modules;

public class EventBroadcasterTest
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly EventBroadcaster _eventBroadcaster;
    private readonly Mock<CopsDbContext> _dbContextMock;
    private readonly Mock<DiscordSocketClient> _socketClientMock;
    private readonly Mock<IImageFetcher> _imageFetcherMock;
    private readonly ServiceCollection _services;

    public EventBroadcasterTest( )
    {
        _imageFetcherMock = _fixture.Freeze<Mock<IImageFetcher>>();
        _socketClientMock = _fixture.Freeze<Mock<DiscordSocketClient>>( );

        _dbContextMock = _fixture.Freeze<Mock<CopsDbContext>>();
        _dbContextMock.Setup( m => m.SaveChangesAsync( default ) )
            .ReturnsAsync( 0 );
        _dbContextMock.Setup( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ) )
            .ReturnsAsync( 0 );

        _services = new ServiceCollection();
        _fixture.Inject( _services );
        _services.AddSingleton( _imageFetcherMock.Object );
        _services.AddSingleton( _socketClientMock.Object );
        _services.AddSingleton( _dbContextMock.Object );
        _fixture.Inject( _services.BuildServiceProvider() as IServiceProvider );
        
        _eventBroadcaster = _fixture.Create<EventBroadcaster>();
    }

    [Theory]
    [AutoDomainData]
    public async Task BroadcastStatusChange_ForAllSubscriptions(
        List<UserSubscription> userSubscriptions, 
        List<ChannelSubscription> channelSubscriptions, 
        Proposal proposal,
        string newStatus)
    {
        channelSubscriptions.ForEach( s => s.Chain = proposal.Chain );
        userSubscriptions.ForEach( s => s.Chain = proposal.Chain );
        
        _dbContextMock.Setup( m => m.UserSubscriptions )
            .ReturnsDbSet(userSubscriptions);
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .ReturnsDbSet(channelSubscriptions);

        await _eventBroadcaster.BroadcastStatusChangeAsync( proposal, newStatus );
        
        _socketClientMock.Verify( m => m.GetUser( It.IsAny<ulong>() ), Times.Exactly( userSubscriptions.Count ) );
        _socketClientMock.Verify( m => m.GetGuild( It.IsAny<ulong>() ), Times.Exactly( channelSubscriptions.Count ) );
    }

    [Theory]
    [AutoDomainData]
    public async Task BroadcastNewUpgradeAsync_WrongPropType(
        Proposal proposal,
        ProposalInfoUpgradePlan plan)
    {
        await _eventBroadcaster.BroadcastNewUpgradeAsync( proposal, Constants.ProposalStatusPassed, plan );
     
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Never );
        _socketClientMock.Verify( m => m.GetUser( It.IsAny<ulong>() ), Times.Never );
        _socketClientMock.Verify( m => m.GetGuild( It.IsAny<ulong>() ), Times.Never );
    }

    [Theory]
    [AutoDomainData]
    public async Task BroadcastNewUpgradeAsync_NeitherVotingNorPassed(
        Proposal proposal,
        ProposalInfoUpgradePlan plan)
    {
        proposal.ProposalType = Constants.ProposalTypeSoftwareUpgrade;
        
        await _eventBroadcaster.BroadcastNewUpgradeAsync( proposal, Constants.ProposalStatusDepositPeriod, plan );
     
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Never );
        _socketClientMock.Verify( m => m.GetUser( It.IsAny<ulong>() ), Times.Never );
        _socketClientMock.Verify( m => m.GetGuild( It.IsAny<ulong>() ), Times.Never );
    }

    [Theory]
    [InlineData( Constants.ProposalStatusDepositPeriod, Constants.ProposalStatusPassed, false )]
    [InlineData( Constants.ProposalStatusDepositPeriod, Constants.ProposalStatusVotingPeriod, false )]
    [InlineData( Constants.ProposalStatusVotingPeriod, Constants.ProposalStatusPassed, true )]
    [InlineData( Constants.ProposalStatusVotingPeriod, Constants.ProposalStatusRejected, true )]
    [InlineData( Constants.ProposalStatusPassed, Constants.ProposalStatusRejected, true )]
    public async Task BroadcastNewUpgradeAsync_( string previousStatus, string newStatus, bool shouldSkip )
    {
        var proposal = new Proposal()
        {
            ProposalType = Constants.ProposalTypeSoftwareUpgrade,
            Status = previousStatus
        };
        _dbContextMock.Setup( m => m.Proposals )
            .ReturnsDbSet(new List<Proposal> { proposal });
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .ReturnsDbSet(new List<ChannelSubscription>());
        _dbContextMock.Setup( m => m.TrackedEvents )
            .ReturnsDbSet(new List<TrackedEvent>());
        
        await _eventBroadcaster.BroadcastNewUpgradeAsync( proposal, newStatus, new ProposalInfoUpgradePlan(){ Height = $"{1000}"} );
     
        _dbContextMock.Verify( m => m.Proposals, shouldSkip ? Times.Never : Times.Once );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, shouldSkip ? Times.Never : Times.Once );
    }

    [Theory]
    [AutoDomainData]
    public async Task BroadcastNewUpgradeAsync_NoSubscriptions(
        Proposal proposal,
        ProposalInfoUpgradePlan plan)
    {
        plan.Height = $"{1000L}";
        proposal.ProposalType = Constants.ProposalTypeSoftwareUpgrade;
        _dbContextMock.Setup( m => m.Proposals )
            .ReturnsDbSet(new List<Proposal> { proposal });
        _dbContextMock.Setup( m => m.ChannelSubscriptions )
            .ReturnsDbSet(new List<ChannelSubscription>());
        _dbContextMock.Setup( m => m.TrackedEvents )
            .ReturnsDbSet(new List<TrackedEvent>());
        
        await _eventBroadcaster.BroadcastNewUpgradeAsync( proposal, Constants.ProposalStatusPassed, plan );
     
        _dbContextMock.Verify( m => m.Proposals, Times.Once );
        _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Once );
        _dbContextMock.Verify( m => m.TrackedEvents, Times.Once );
        _socketClientMock.Verify( m => m.GetUser( It.IsAny<ulong>() ), Times.Never );
        _socketClientMock.Verify( m => m.GetGuild( It.IsAny<ulong>() ), Times.Never );
    }

    // [Theory]
    // [AutoDomainData]
    // public async Task BroadcastNewUpgradeAsync_ForAllSubscriptions(
    //     List<ChannelSubscription> channelSubscriptions, 
    //     Proposal proposal,
    //     ProposalInfoUpgradePlan plan)
    // {
    //     plan.Height = $"{1000L}";
    //     proposal.ProposalType = Constants.ProposalTypeSoftwareUpgrade;
    //     channelSubscriptions.ForEach( s => s.Chain = proposal.Chain );
    //     _dbContextMock.Setup( m => m.ChannelSubscriptions )
    //         .ReturnsDbSet(channelSubscriptions);
    //     _dbContextMock.Setup( m => m.AddAsync( It.IsAny<TrackedEvent>(), default ) )
    //         .ReturnsAsync( new EntityEntry<TrackedEvent>( null ) );
    //     _dbContextMock.Setup( m => m.AddAsync( It.IsAny<TrackedEventThread>(), default ) )
    //         .ReturnsAsync( new EntityEntry<TrackedEventThread>( null ) );
    //
    //     var constr = typeof(SocketGuild).GetConstructor( BindingFlags.Instance | BindingFlags.NonPublic,
    //         new[] { typeof(DiscordSocketClient), typeof(ulong) } );
    //     var socketGuild = constr.Invoke( new object[] { _socketClientMock.Object, 1000uL } ) as SocketGuild;
    //     
    //     // var obj = Activator.CreateInstance( typeof(SocketGuild), 
    //     //     BindingFlags.Instance | BindingFlags.NonPublic, 
    //     //     null, 
    //     //     new object[] { _socketClientMock.Object, 0L } );
    //     // var obj = new PrivateObject( typeof(SocketGuild) );
    //     // var sg = obj.Invoke( "Create", BindingFlags.Static, new object[] { _socketClientMock.Object, 0L } );
    //     // var obj = new PrivateObject( typeof(SocketGuild),
    //     //     new[] { typeof(DiscordSocketClient), typeof(ulong) },
    //     //     new object[] { _socketClientMock.Object, 0L } );
    //     // obj
    //     // var foo = new SocketGuild( new DiscordSocketClient(), 0L );
    //     
    //     var guildMock = _fixture.Freeze<Mock<SocketGuild>>();
    //     _socketClientMock.Setup( m => m.GetGuild( It.IsAny<ulong>() ) )
    //         .Returns( socketGuild as SocketGuild );
    //     
    //     await _eventBroadcaster.BroadcastNewUpgradeAsync( proposal, "PROPOSAL_STATUS_PASSED", plan );
    //  
    //     _dbContextMock.Verify( m => m.ChannelSubscriptions, Times.Once );
    //     _dbContextMock.Verify( m => m.AddAsync( It.IsAny<TrackedEvent>(), default ), Times.Once );
    //     _dbContextMock.Verify( m => m.AddAsync( It.IsAny<TrackedEventThread>(), default ), Times.Once );
    //     _socketClientMock.Verify( m => m.GetGuild( It.IsAny<ulong>() ), Times.Exactly(channelSubscriptions.Count) );
    //     _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
    // }
}

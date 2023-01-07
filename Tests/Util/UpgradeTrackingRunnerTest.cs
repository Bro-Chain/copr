using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using CosmosProposalBot.Modules;
using CosmosProposalBot.Services;
using CosmosProposalBot.Util;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Util;

public class UpgradeTrackingRunnerTest
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly UpgradeTrackingRunner _runner;
    private readonly ServiceCollection _services;
    private readonly Mock<CopsDbContext> _dbContextMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IApiRequestHelper> _apiRequestHelper;
    private readonly Mock<IEventBroadcaster> _eventBroadcaster;

    public UpgradeTrackingRunnerTest( )
    {
        _apiRequestHelper = _fixture.Freeze<Mock<IApiRequestHelper>>();
        _httpClientFactoryMock = _fixture.Freeze<Mock<IHttpClientFactory>>();
        _eventBroadcaster = _fixture.Freeze<Mock<IEventBroadcaster>>();

        _dbContextMock = _fixture.Freeze<Mock<CopsDbContext>>();
        _dbContextMock.Setup( m => m.SaveChangesAsync( default ) )
            .ReturnsAsync( 0 );

        _services = new ServiceCollection();
        _fixture.Inject( _services );
        _services.AddSingleton( _httpClientFactoryMock.Object );
        _services.AddSingleton( _dbContextMock.Object );
        _services.AddSingleton( _apiRequestHelper.Object );
        _services.AddSingleton( _eventBroadcaster.Object );
        _fixture.Inject( _services.BuildServiceProvider() as IServiceProvider );

        _runner = _fixture.Create<UpgradeTrackingRunner>();
    }

    private void CommonSetup( ulong propHeight, Proposal proposal, List<TrackedEvent> trackedEvents, bool stillPending )
    {
        var trackedEventsDbSet = trackedEvents.AsQueryable().BuildMockDbSet();
        _dbContextMock.Setup( m => m.TrackedEvents )
            .Returns( trackedEventsDbSet.Object );
        _apiRequestHelper
            .Setup( m => 
                m.GetBlockHeaderViaRest( 
                    It.IsAny<IHttpClientFactory>(), 
                    proposal.Chain.Endpoints, 
                    proposal.Chain.Name, 
                    It.IsAny<CancellationToken>(), 
                    "latest" ) )
            .ReturnsAsync( ( ) => ( 
                true, 
                new BlockInfoHeader()
                {
                    Height = stillPending ? $"{propHeight - 1000}" : $"{propHeight + 2000}",
                    ChainId = proposal.Chain.ChainId,
                    Time = DateTime.UtcNow
                }, 
                new Endpoint() ) )
            .Verifiable();
        _apiRequestHelper
            .Setup( m => 
                m.GetBlockHeaderViaRest( 
                    It.IsAny<IHttpClientFactory>(), 
                    proposal.Chain.Endpoints, 
                    proposal.Chain.Name, 
                    It.IsAny<CancellationToken>(), 
                    stillPending ? $"{propHeight - 2000}" : $"{propHeight + 1000}" ) )
            .ReturnsAsync( ( ) => ( 
                true, 
                new BlockInfoHeader()
                {
                    Height = stillPending ? $"{propHeight - 2000}" : $"{propHeight + 1000}",
                    ChainId = proposal.Chain.ChainId,
                    Time = DateTime.UtcNow
                }, 
                new Endpoint() ) )
            .Verifiable();
        _dbContextMock.Setup( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ) )
            .ReturnsAsync( 0 );
    }

    [Fact]
    public async Task Run_NoEvents()
    {
        _dbContextMock.Setup( m => m.TrackedEvents )
            .Returns( new List<TrackedEvent>().AsQueryable().BuildMockDbSet().Object );

        var cts = new CancellationTokenSource();
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.TrackedEvents, Times.Once );
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_NoPendingEvents( ulong height )
    {
        var trackedEvents = new List<TrackedEvent>
        {
            new ()
            {
                Height = height,
                Status = TrackedEventStatus.Passed
            }
        };
        _dbContextMock.Setup( m => m.TrackedEvents )
            .Returns( trackedEvents.AsQueryable().BuildMockDbSet().Object );

        var cts = new CancellationTokenSource();
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.TrackedEvents, Times.Once );
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_PendingEventThatPassed( Proposal proposal )
    {
        ulong propHeight = 10000;
        var cts = new CancellationTokenSource();
        var trackedEvents = new List<TrackedEvent>
        {
            new ()
            {
                Height = propHeight,
                Status = TrackedEventStatus.Pending,
                Proposal = proposal
            }
        };
        CommonSetup( propHeight, proposal, trackedEvents, false );
        
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.TrackedEvents, Times.Exactly(2) );
        _apiRequestHelper.Verify();
        _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
        trackedEvents.First().Status.Should().Be( TrackedEventStatus.Passed );
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_PendingEventEstimateUpdated( Proposal proposal )
    {
        ulong propHeight = 10000;
        var cts = new CancellationTokenSource();
        var trackedEvents = new List<TrackedEvent>
        {
            new ()
            {
                Height = propHeight,
                Status = TrackedEventStatus.Pending,
                Proposal = proposal
            }
        };
        CommonSetup( propHeight, proposal, trackedEvents, true );
        
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.TrackedEvents, Times.Exactly(2) );
        _apiRequestHelper.Verify();
        _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
        trackedEvents.First().Status.Should().Be( TrackedEventStatus.Pending );
        trackedEvents.First().HeightEstimatedAt.Should().NotBeNull();
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_PendingEventLastUpdate( Proposal proposal )
    {
        ulong propHeight = 10000;
        var cts = new CancellationTokenSource();
        var trackedEvents = new List<TrackedEvent>
        {
            new ()
            {
                Height = propHeight,
                Status = TrackedEventStatus.Pending,
                Proposal = proposal,
                NextNotificationAtSecondsLeft = 60*2
            }
        };
        CommonSetup( propHeight, proposal, trackedEvents, true );
        
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.TrackedEvents, Times.Exactly(2) );
        _apiRequestHelper.Verify();
        _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
        trackedEvents.First().Status.Should().Be( TrackedEventStatus.Passed );
        trackedEvents.First().HeightEstimatedAt.Should().NotBeNull();
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_PendingEventPendingBroadcast( Proposal proposal )
    {
        ulong propHeight = 10000;
        var cts = new CancellationTokenSource();
        var trackedEvents = new List<TrackedEvent>
        {
            new ()
            {
                Height = propHeight,
                Status = TrackedEventStatus.Pending,
                Proposal = proposal
            }
        };
        CommonSetup( propHeight, proposal, trackedEvents, true );
        
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.TrackedEvents, Times.Exactly(2) );
        _apiRequestHelper.Verify();
        _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
        trackedEvents.First().Status.Should().Be( TrackedEventStatus.Pending );
        _eventBroadcaster.Verify( m => m.BroadcastUpgradeReminderAsync( It.IsAny<TrackedEvent>() ), Times.Once);
    }
}

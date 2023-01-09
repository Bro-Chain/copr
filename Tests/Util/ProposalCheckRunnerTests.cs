using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using Moq.Contrib.HttpClient;
using Moq.EntityFrameworkCore;
using Newtonsoft.Json;
using Xunit;

namespace Tests.Util;

public class ProposalCheckRunnerTests
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly ProposalCheckRunner _runner;
    private readonly ServiceCollection _services;
    private readonly Mock<CopsDbContext> _dbContextMock;
    private readonly Mock<IApiRequestHelper> _apiRequestHelper;
    private readonly Mock<IEventBroadcaster> _eventBroadcaster;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;

    public ProposalCheckRunnerTests( )
    {
        _apiRequestHelper = _fixture.Freeze<Mock<IApiRequestHelper>>();
        _httpMessageHandler = new Mock<HttpMessageHandler>( MockBehavior.Strict );
        _fixture.Inject( _httpMessageHandler );
        _httpClient = _httpMessageHandler.CreateClient();
        _httpClientFactoryMock = _fixture.Freeze<Mock<IHttpClientFactory>>();
        _httpClientFactoryMock.Setup( m => m.CreateClient( It.IsAny<string>() ) )
            .Returns( _httpClient );
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

        _runner = _fixture.Create<ProposalCheckRunner>();
    }

    [Fact]
    public async Task Run_NoChains()
    {
        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet( new List<Chain>() );

        var cts = new CancellationTokenSource();
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.Chains, Times.Once );
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_RestEmptyResponse( Chain chain, BlockInfoResult blockInfoResult, ProposalInfoResponse proposalInfo )
    {
        chain.Endpoints = new List<Endpoint>()
        {
            new ()
            {
                Chain = chain,
                Url = "http://localhost:1317",
                Provider = "Test",
                Type = EndpointType.Rest
            }
        };

        chain.Proposals = chain.Proposals.Take( 1 ).ToList();
        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet(new List<Chain> { chain });

        proposalInfo.Proposals = proposalInfo.Proposals.Take( 1 ).ToList();
        proposalInfo.Proposals.First().Id = chain.Proposals.First().ProposalId;
        proposalInfo.Proposals.First().Status = chain.Proposals.First().Status;

        blockInfoResult.Block.Header.Height = $"{1000L}";
        blockInfoResult.Block.Header.Time = DateTime.UtcNow.AddSeconds(-5);
        _httpMessageHandler.SetupAnyRequestSequence()
            .ReturnsResponse( JsonConvert.SerializeObject( new {} ) )
            .ReturnsResponse( JsonConvert.SerializeObject( proposalInfo ) );

        var cts = new CancellationTokenSource();
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 500, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 500, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.Chains, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.Proposals, Times.Never );
        _eventBroadcaster.Verify( m => m.BroadcastStatusChangeAsync( It.IsAny<Proposal>(), It.IsAny<string>() ), Times.Never);
        _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_RestNotImplemented( Chain chain, BlockInfoResult blockInfoResult, ProposalInfoResponse proposalInfo )
    {
        chain.Endpoints = new List<Endpoint>()
        {
            new ()
            {
                Chain = chain,
                Url = "http://localhost:1317",
                Provider = "Test",
                Type = EndpointType.Rest
            }
        };

        chain.Proposals = chain.Proposals.Take( 1 ).ToList();
        var chains = new List<Chain> { chain };
        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet(chains);

        proposalInfo.Proposals = proposalInfo.Proposals.Take( 1 ).ToList();
        proposalInfo.Proposals.First().Id = chain.Proposals.First().ProposalId;
        proposalInfo.Proposals.First().Status = chain.Proposals.First().Status;

        blockInfoResult.Block.Header.Height = $"{1000L}";
        blockInfoResult.Block.Header.Time = DateTime.UtcNow.AddSeconds(-5);
        _httpMessageHandler.SetupAnyRequestSequence()
            .ReturnsResponse( HttpStatusCode.NotImplemented )
            .ReturnsResponse( JsonConvert.SerializeObject( proposalInfo ) );

        var cts = new CancellationTokenSource();
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 500, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 500, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.Chains, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.Proposals, Times.Never );
        _eventBroadcaster.Verify( m => m.BroadcastStatusChangeAsync( It.IsAny<Proposal>(), It.IsAny<string>() ), Times.Never);
        _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_ExistingProposalSameStatus( Chain chain, BlockInfoResult blockInfoResult, ProposalInfoResponse proposalInfo )
    {
        chain.Endpoints = new List<Endpoint>()
        {
            new ()
            {
                Chain = chain,
                Url = "http://localhost:1317",
                Provider = "Test",
                Type = EndpointType.Rest
            }
        };
        
        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet(new List<Chain> { chain });

        proposalInfo.Proposals = proposalInfo.Proposals.Take( 1 ).ToList();
        proposalInfo.Proposals.First().Id = chain.Proposals.First().ProposalId;
        proposalInfo.Proposals.First().Status = chain.Proposals.First().Status;

        blockInfoResult.Block.Header.Height = $"{1000L}";
        blockInfoResult.Block.Header.Time = DateTime.UtcNow.AddSeconds(-5);
        _httpMessageHandler.SetupAnyRequestSequence()
            .ReturnsResponse( JsonConvert.SerializeObject( blockInfoResult ) )
            .ReturnsResponse( JsonConvert.SerializeObject( proposalInfo ) );

        var cts = new CancellationTokenSource();
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.Chains, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.Proposals, Times.Never );
        _eventBroadcaster.Verify( m => m.BroadcastStatusChangeAsync( It.IsAny<Proposal>(), It.IsAny<string>() ), Times.Never);
        _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_ExistingProposalNewStatus( Chain chain, BlockInfoResult blockInfoResult, ProposalInfoResponse proposalInfo )
    {
        chain.Endpoints = new List<Endpoint>()
        {
            new ()
            {
                Chain = chain,
                Url = "http://localhost:1317",
                Provider = "Test",
                Type = EndpointType.Rest
            }
        };
        
        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet(new List<Chain> { chain });

        proposalInfo.Proposals = proposalInfo.Proposals.Take( 1 ).ToList();
        proposalInfo.Proposals.First().Id = chain.Proposals.First().ProposalId;
        proposalInfo.Proposals.First().SubmitTime = 
        proposalInfo.Proposals.First().DepositEndTime = 
        proposalInfo.Proposals.First().VotingEndTime = 
        proposalInfo.Proposals.First().VotingStartTIme = DateTime.UtcNow.ToString("u");

        blockInfoResult.Block.Header.Height = $"{1000L}";
        blockInfoResult.Block.Header.Time = DateTime.UtcNow.AddSeconds(-5);
        _httpMessageHandler.SetupAnyRequestSequence()
            .ReturnsResponse( JsonConvert.SerializeObject( blockInfoResult ) )
            .ReturnsResponse( JsonConvert.SerializeObject( proposalInfo ) );

        var cts = new CancellationTokenSource();
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.Chains, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.Proposals, Times.Never );
        _eventBroadcaster.Verify( m => m.BroadcastStatusChangeAsync( It.IsAny<Proposal>(), It.IsAny<string>() ), Times.Once);
        _eventBroadcaster.Verify( m => m.BroadcastNewUpgradeAsync( It.IsAny<Proposal>(), It.IsAny<string>(), It.IsAny<ProposalInfoUpgradePlan>() ), Times.Once);
        chain.Proposals.First().Status.Should().Be( proposalInfo.Proposals.First().Status );
        _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
    }

    [Theory]
    [AutoDomainData]
    public async Task Run_NewProposal( Chain chain, BlockInfoResult blockInfoResult, ProposalInfoResponse proposalInfo )
    {
        chain.Endpoints = new List<Endpoint>()
        {
            new ()
            {
                Chain = chain,
                Url = "http://localhost:1317",
                Provider = "Test",
                Type = EndpointType.Rest
            }
        };
        chain.Proposals.Clear();
        
        _dbContextMock.Setup( m => m.Chains )
            .ReturnsDbSet(new List<Chain> { chain });
        _dbContextMock.Setup( m => m.Proposals )
            .ReturnsDbSet(new List<Proposal>());

        proposalInfo.Proposals = proposalInfo.Proposals.Take( 1 ).ToList();
        proposalInfo.Proposals.First().SubmitTime = 
        proposalInfo.Proposals.First().DepositEndTime = 
        proposalInfo.Proposals.First().VotingEndTime = 
        proposalInfo.Proposals.First().VotingStartTIme = DateTime.UtcNow.ToString("u");
        
        blockInfoResult.Block.Header.Height = $"{1000L}";
        blockInfoResult.Block.Header.Time = DateTime.UtcNow.AddSeconds(-5);
        _httpMessageHandler.SetupAnyRequestSequence()
            .ReturnsResponse( JsonConvert.SerializeObject( blockInfoResult ) )
            .ReturnsResponse( JsonConvert.SerializeObject( proposalInfo ) );

        var cts = new CancellationTokenSource();
        var runTask = _runner.RunAsync( cts.Token );
        
        await Task.Delay( 250, CancellationToken.None );
        cts.Cancel();
        await Task.Delay( 250, CancellationToken.None );

        runTask.IsCanceled.Should().BeTrue();
        _dbContextMock.Verify( m => m.Chains, Times.Exactly(2) );
        _dbContextMock.Verify( m => m.Proposals, Times.Once );
        _eventBroadcaster.Verify( m => m.BroadcastStatusChangeAsync( It.IsAny<Proposal>(), It.IsAny<string>() ), Times.Once);
        _eventBroadcaster.Verify( m => m.BroadcastNewUpgradeAsync( It.IsAny<Proposal>(), It.IsAny<string>(), It.IsAny<ProposalInfoUpgradePlan>() ), Times.Once);
        chain.Proposals.Count.Should().Be( 1 );
        chain.Proposals.First().Status.Should().Be( proposalInfo.Proposals.First().Status );
        _dbContextMock.Verify( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ), Times.Once );
    }
}

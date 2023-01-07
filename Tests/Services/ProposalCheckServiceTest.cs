using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Services;
using CosmosProposalBot.Util;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Tests.Services;

public class ProposalCheckServiceTest
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly ProposalCheckService _service;
    private readonly Mock<IProposalCheckRunner> _proposalCheckRunnerMock;
    private readonly Mock<IHostEnvironment> _hostEnvironmentMock;

    public ProposalCheckServiceTest()
    {
        _hostEnvironmentMock = _fixture.Freeze<Mock<IHostEnvironment>>();
        _proposalCheckRunnerMock = _fixture.Freeze<Mock<IProposalCheckRunner>>();
        _service = _fixture.Create<ProposalCheckService>();
    }

    [Fact]
    public async Task StartAsync_RunnerStarted()
    {
        await _service.StartAsync( CancellationToken.None );
        _proposalCheckRunnerMock.Verify( m => m.RunAsync( It.IsAny<CancellationToken>() ), Times.Once);
    }
}

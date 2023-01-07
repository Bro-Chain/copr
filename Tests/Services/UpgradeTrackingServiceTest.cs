using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Services;
using CosmosProposalBot.Util;
using Moq;
using Xunit;

namespace Tests.Services;

public class UpgradeTrackingServiceTest
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly UpgradeTrackingService _service;
    private readonly Mock<IUpgradeTrackingRunner> _upgradeTrackingRunnerMock;

    public UpgradeTrackingServiceTest()
    {
        _upgradeTrackingRunnerMock = _fixture.Freeze<Mock<IUpgradeTrackingRunner>>();
        _service = _fixture.Create<UpgradeTrackingService>();
    }

    [Fact]
    public async Task StartAsync_RunnerStarted( )
    {
        await _service.StartAsync( CancellationToken.None );
        _upgradeTrackingRunnerMock.Verify( m => m.RunAsync( It.IsAny<CancellationToken>() ), Times.Once);
    }
}

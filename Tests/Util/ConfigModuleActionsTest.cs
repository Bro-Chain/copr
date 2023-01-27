using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Data;
using CosmosProposalBot.Util;
using Discord;
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

    public ConfigModuleActionsTest( )
    {
        _interactionContextMock = _fixture.Freeze<Mock<IInteractionContext>>();
        _interactionMock = _fixture.Freeze<Mock<IDiscordInteraction>>();

        _dbContextMock = _fixture.Freeze<Mock<CopsDbContext>>();
        _dbContextMock.Setup( m => m.SaveChangesAsync( default ) )
            .ReturnsAsync( 0 );
        _dbContextMock.Setup( m => m.SaveChangesAsync( It.IsAny<CancellationToken>() ) )
            .ReturnsAsync( 0 );

        _configModuleActions = _fixture.Create<ConfigModuleActions>();
    }
    
    [Fact]
    // [AutoDomainData]
    public async Task AllowRole_Something()
    {
        // verify that the role is added to the list of allowed roles
        
    }
}

using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using AutoFixture;
using CosmosProposalBot.Modules;
using CosmosProposalBot.Util;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Tests.Modules;

public class ConfigModuleTest
{
    private readonly IFixture _fixture = FixtureFactory.CreateFixture();
    private readonly ConfigModule _configModule;
    private readonly Mock<IConfigModuleActions> _configModuleActionsMock;
    private readonly ServiceCollection _services;

    public ConfigModuleTest( )
    {
        _configModuleActionsMock = _fixture.Freeze<Mock<IConfigModuleActions>>();

        _services = new ServiceCollection();
        _fixture.Inject( _services );
        _services.AddSingleton( _configModuleActionsMock.Object );
        _fixture.Inject( _services.BuildServiceProvider() as IServiceProvider );
        
        _configModule = _fixture.Create<ConfigModule>();
    }
    
    [Theory]
    [AutoDomainData]
    public async Task AllowRole( IRole role )
    {
        await _configModule.AllowRole( role );

        _configModuleActionsMock.Verify( m => m.AllowRoleAsync( It.IsAny<IInteractionContext>(), role ), Times.Once );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task RevokeRole( IRole role )
    {
        await _configModule.RevokeRole( role );

        _configModuleActionsMock.Verify( m => m.RevokeRoleAsync( It.IsAny<IInteractionContext>(), role ), Times.Once );
    }

    [Fact]
    public async Task ListRoles()
    {
        await _configModule.ListRoles();

        _configModuleActionsMock.Verify( m => m.ListRolesAsync( It.IsAny<IInteractionContext>() ), Times.Once );
    }
    
    [Fact]
    public async Task AddEndpoint()
    {
        await _configModule.AddEndpoint();

        _configModuleActionsMock.Verify( m => m.AddEndpointAsync( It.IsAny<IInteractionContext>() ), Times.Once );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task RemoveEndpoint( string chainName, string providerName )
    {
        await _configModule.RemoveEndpoint( chainName, providerName );

        _configModuleActionsMock.Verify( m => m.RemoveEndpointAsync( It.IsAny<IInteractionContext>(), chainName, providerName  ), Times.Once );
    }
    
    [Fact]
    public async Task AddCustomChain()
    {
        await _configModule.AddCustomChain();

        _configModuleActionsMock.Verify( m => m.AddCustomChainAsync( It.IsAny<IInteractionContext>() ), Times.Once );
    }
    
    [Theory]
    [AutoDomainData]
    public async Task RemoveCustomChain( string chainName )
    {
        await _configModule.RemoveCustomChain(chainName);

        _configModuleActionsMock.Verify( m => m.RemoveCustomChainAsync( It.IsAny<IInteractionContext>(), chainName ), Times.Once );
    }

}

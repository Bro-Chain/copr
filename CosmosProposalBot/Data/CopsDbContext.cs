using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace CosmosProposalBot.Data;

public class CopsDbContext : DbContext
{
    public virtual DbSet<ChannelSubscription> ChannelSubscriptions { get; set; }
    public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }
    public virtual DbSet<Chain> Chains { get; set; }
    public virtual DbSet<Guild> Guilds { get; set; }
    public virtual DbSet<Endpoint> Endpoints { get; set; }
    public virtual DbSet<Proposal> Proposals { get; set; }
    public virtual DbSet<TrackedEvent> TrackedEvents { get; set; }

    public CopsDbContext( DbContextOptions<CopsDbContext> options )
        :base( options )
    {
        
    }
    
    public static async Task Migrate(IServiceProvider provider)
    {
        var context = provider.GetRequiredService<CopsDbContext>();
        await context.Database.MigrateAsync();
        await context.SaveChangesAsync();
    }
    
    public class CopsFactory : IDesignTimeDbContextFactory<CopsDbContext>
    {
        public CopsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CopsDbContext>();
            optionsBuilder.UseSqlServer("Server=localhost,9433;Database=cops;User ID=sa;Password=MyPassword!1;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;");

            return new CopsDbContext(optionsBuilder.Options);
        }
    }
}

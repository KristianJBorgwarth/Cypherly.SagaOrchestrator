using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SagaOrchestrator.Infrastructure.Persistence.Context;

namespace SagaOrchestrator.Infrastructure.Extensions;

internal static class PersistenceExtensions
{
    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SagaOrchestratorDbConnectionString");

        services.AddDbContext<OrchestratorDbContext>(options =>
        {
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(OrchestratorDbContext).Assembly.FullName));
        });
    }
}
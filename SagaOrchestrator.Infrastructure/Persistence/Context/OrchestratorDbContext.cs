using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using SagaOrchestrator.Infrastructure.Persistence.ModelConfigurations;

namespace SagaOrchestrator.Infrastructure.Persistence.Context;

public sealed class OrchestratorDbContext(DbContextOptions options) : SagaDbContext(options)
{
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get
        {
            yield return new UserDeleteSagaModelConfiguration();
        }
    }
}
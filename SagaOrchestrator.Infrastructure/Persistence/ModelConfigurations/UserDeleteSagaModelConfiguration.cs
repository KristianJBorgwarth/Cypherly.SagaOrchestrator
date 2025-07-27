using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagaOrchestrator.Application.Sagas;

namespace SagaOrchestrator.Infrastructure.Persistence.ModelConfigurations;

public sealed class UserDeleteSagaModelConfiguration : SagaClassMap<UserDeleteSagaState>
{
    protected override void Configure(EntityTypeBuilder<UserDeleteSagaState> e, ModelBuilder model)
    {
        e.ToTable("user_delete_saga");
    }
}
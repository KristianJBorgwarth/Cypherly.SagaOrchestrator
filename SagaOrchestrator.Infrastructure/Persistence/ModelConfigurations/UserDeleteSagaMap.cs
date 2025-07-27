using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagaOrchestrator.Application.Sagas;

namespace SagaOrchestrator.Infrastructure.Persistence.ModelConfigurations;

public sealed class UserDeleteSagaMap : SagaClassMap<UserDeleteSagaState>
{
    protected override void Configure(EntityTypeBuilder<UserDeleteSagaState> entity, ModelBuilder model)
    {
        entity.ToTable("user_delete_saga");
    }
}
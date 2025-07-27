using Cypherly.Message.Contracts.Enums;
using Cypherly.Message.Contracts.Messages.Common;
using Cypherly.Message.Contracts.Messages.Email;
using Cypherly.Message.Contracts.Messages.User;
using MassTransit;
using Microsoft.Extensions.Logging;
using SagaOrchestrator.Application.Sagas;

namespace SagaOrchestrator.Infrastructure.StateMachines;

public sealed class UserDeleteSagaMachine : MassTransitStateMachine<UserDeleteSagaState>
{
    public Event<UserDeletedMessage>? UserDeleteMessageReceived { get; private set; }
    public Event<OperationSucceededMessage>? OperationSucceededReceived { get; private set; }
    public Event<Fault<UserDeleteMessage>>? UserProfileDeleteFault { get; private set; }
    public Event<Fault<SendEmailMessage>>? SendEmailFault { get; private set; }
    public State? DeletingUserProfile { get; private set; }
    public State? SendingEmail { get; private set; }
    public State? Failed { get; private set; }
    public State? Finished { get; private set; }

    public UserDeleteSagaMachine(ILogger<UserDeleteSagaMachine> logger)
    {
        InstanceState(x => x.CurrentState);

        Event(() => UserDeleteMessageReceived, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => UserProfileDeleteFault, x => x.CorrelateById(m => m.Message.Message.CorrelationId));
        Event(() => SendEmailFault, x => x.CorrelateById(m => m.Message.Message.CorrelationId));
        Event(() => OperationSucceededReceived, x => x.CorrelateById(m => m.Message.CorrelationId));

        Initially(
            When(UserDeleteMessageReceived)
                .ThenAsync(async context =>
                {
                    context.Saga.SetUserId(context.Message.UserId);
                    context.Saga.SetEmail(context.Message.Email);
                    logger.LogInformation("Received UserDeletedMessage, publishing UserProfileDeleteMessage.");

                    await context.Publish(new UserDeleteMessage
                    {
                        CausationId = context.Message.Id,
                        CorrelationId = context.Message.CorrelationId,
                        UserProfileId = context.Message.UserId
                    });
                })
                .TransitionTo(DeletingUserProfile)
        );

        During(DeletingUserProfile,
            When(UserProfileDeleteFault)
                .ThenAsync(async context =>
                {
                    logger.LogError("UserProfileDelete faulted, rolling back, and failing saga with ID: {ID}.",
                        context.Saga.CorrelationId);

                    context.Saga.SetError(context.Message.Exceptions);

                    await context.Publish(new UserDeleteFailedMessage
                    {
                        CausationId = context.Message.Message.Id,
                        CorrelationId = context.Saga.CorrelationId,
                        UserId = context.Saga.UserId,
                        Services = [ServiceType.AuthenticationService]
                    });

                })
                .TransitionTo(Failed),
            When(OperationSucceededReceived)
                .If(context => context.Message.OperationType == OperationType.UserProfileDelete, binder =>
                    binder.ThenAsync(async context =>
                    {
                        logger.LogInformation("UserProfileDelete succeeded, publishing SendEmailMessage.");

                        await context.Publish(new SendEmailMessage
                        {
                            To = context.Saga.Email ?? throw new NullReferenceException(
                                "Email was null during user profile delete. CorrelationId:" +
                                context.Saga.CorrelationId),
                            Subject = "Cypherly Account Deletion",
                            Body = "You account has been deleted.",
                            CorrelationId = context.Saga.CorrelationId,
                            CausationId = context.Message.Id
                        });

                    }))
                .TransitionTo(SendingEmail));

        During(SendingEmail,
            When(SendEmailFault)
                .ThenAsync(async context =>
                {
                    logger.LogError("SendEmail faulted, rolling back, and failing saga with ID: {ID}.",
                        context.Saga.CorrelationId);

                    context.Saga.SetError(context.Message.Exceptions);


                    await context.Publish(new UserDeleteFailedMessage
                    {
                        CausationId = context.Message.Message.Id,
                        CorrelationId = context.Saga.CorrelationId,
                        UserId = context.Saga.UserId,
                        Services = [ServiceType.AuthenticationService, ServiceType.AuthenticationService]
                    });
                })
                .TransitionTo(Failed),
            When(OperationSucceededReceived)
                .If(context => context.Message.OperationType == OperationType.SendEmail, binder =>
                    binder.Then(context =>
                    {
                        logger.LogInformation("SendEmail succeeded, finalizing saga with ID: {ID}.",
                            context.Saga.CorrelationId);
                    }))
                .TransitionTo(Final)
                .Finalize());

        SetCompletedWhenFinalized();
    }
}
using Cypherly.Message.Contracts.Enums;
using Cypherly.Message.Contracts.Messages.Common;
using Cypherly.Message.Contracts.Messages.Email;
using Cypherly.Message.Contracts.Messages.User;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SagaOrchestrator.Application.Sagas;
using SagaOrchestrator.Infrastructure.SagaStateMachines;

namespace SagaOrchestrator.Test.Unit.SagaTest;

public class UserDeleteSagaTest
{
    [Fact]
    public async Task UserDeleteSaga_Should_Transition_To_DeletingUserProfile_When_Received_UserDeleteMessage()
    {
        // Arrange
        var provider = new ServiceCollection().AddMassTransitTestHarness(cfg =>
        {
            cfg.AddSagaStateMachine<UserDeleteSagaStateMachine, UserDeleteSagaState>()
                .InMemoryRepository();
        }).BuildServiceProvider();

        var harness = provider.GetTestHarness();
        await harness.Start();

        var sagaHarness = harness.GetSagaStateMachineHarness<UserDeleteSagaStateMachine, UserDeleteSagaState>();
        var correlationId = Guid.NewGuid();


        // Act
        var message = new UserDeletedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "test@mail.dk",
            CorrelationId = correlationId
        };
        await harness.Bus.Publish(message);

        // Assert
        var result = await sagaHarness.Consumed.Any<UserDeletedMessage>();
        result.Should().BeTrue();
        var instance = sagaHarness.Created.Contains(correlationId);
        instance.CurrentState.Should().Be("DeletingUserProfile");
        await harness.Stop();
    }

    [Fact]
    public async Task UserDeleteSaga_Should_Transition_To_Failed_When_Received_UserProfileDeleteFault()
    {
        // Arrange
        var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<UserDeleteSagaStateMachine, UserDeleteSagaState>()
                    .InMemoryRepository();

                // Add a consumer that fails when processing UserProfileDeleteMessage to simulate a fault
                cfg.AddConsumer<FaultyUserProfileDeleteConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var sagaHarness = harness.GetSagaStateMachineHarness<UserDeleteSagaStateMachine, UserDeleteSagaState>();
        var correlationId = Guid.NewGuid();

        // Act
        // 1. Publish UserDeletedMessage to create the saga instance and transition to DeletingUserProfile
        var userDeletedMessage = new UserDeletedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "test@mail.dk",
            CorrelationId = correlationId
        };
        
        await harness.Bus.Publish(userDeletedMessage);

        // Assert that the saga instance is created and in the DeletingUserProfile state
        (await sagaHarness.Consumed.Any<UserDeletedMessage>()).Should().BeTrue();

        var instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("DeletingUserProfile");

        // 2. Publish UserProfileDeleteMessage, which will fail in the FaultyUserProfileDeleteConsumer
        var userProfileDeleteMessage = new UserDeleteMessage
        {
            CorrelationId = correlationId,
            UserProfileId = Guid.NewGuid(),
        };
        
        await harness.Bus.Publish(userProfileDeleteMessage);

        // Assert that the saga instance transitioned to Failed state due to the fault
        (await sagaHarness.Consumed.Any<Fault<UserDeleteMessage>>()).Should().BeTrue();

        instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("Failed");
        await harness.Stop();
    }

    [Fact]
    public async Task UserDeleteSaga_Should_Transition_To_Finished_When_Received_OperationSuccededMessage()
    {
        // Arrange
        var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<UserDeleteSagaStateMachine, UserDeleteSagaState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var sagaHarness = harness.GetSagaStateMachineHarness<UserDeleteSagaStateMachine, UserDeleteSagaState>();
        var correlationId = Guid.NewGuid();

        // Act
        // 1. Publish UserDeletedMessage to create the saga instance and transition to DeletingUserProfile
        var userDeletedMessage = new UserDeletedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "test@mail.dk",
            CorrelationId = correlationId
        };
        await harness.Bus.Publish(userDeletedMessage);

        // Assert that the saga instance is created and in the DeletingUserProfile state
        (await sagaHarness.Consumed.Any<UserDeletedMessage>()).Should().BeTrue();

        var instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("DeletingUserProfile");

        // 2. Publish OperationSucceededMessage to transition to SendingEmail state
        var operationSucceededMessage = new OperationSucceededMessage()
        {
            CorrelationId = correlationId,
            OperationType = OperationType.UserProfileDelete
        };
        
        await harness.Bus.Publish(operationSucceededMessage);

        // Assert that the saga instance transitioned to Sending Email state due to the OperationSucceededMessage
        (await sagaHarness.Consumed.Any<OperationSucceededMessage>()).Should().BeTrue();

        instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("SendingEmail");
    }

    [Fact]
    public async Task UserDeleteSaga_Should_Transition_To_Failed_When_Sending_Email_Fails()
    {
        // Arrange
        var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<UserDeleteSagaStateMachine, UserDeleteSagaState>()
                    .InMemoryRepository();

                // Add a consumer that fails when processing SendEmailMessage to simulate a fault
                cfg.AddConsumer<FaultySendEmailConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var sagaHarness = harness.GetSagaStateMachineHarness<UserDeleteSagaStateMachine, UserDeleteSagaState>();
        var correlationId = Guid.NewGuid();

        // Act
        // 1. Publish UserDeletedMessage to create the saga instance and transition to DeletingUserProfile
        var userDeletedMessage = new UserDeletedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "test@mail.dk",
            CorrelationId = correlationId
        };
        await harness.Bus.Publish(userDeletedMessage);

        // Assert that the saga instance is created and in the DeletingUserProfile state
        (await sagaHarness.Consumed.Any<UserDeletedMessage>()).Should().BeTrue();

        var instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("DeletingUserProfile");

        // 2. Publish OperationSucceededMessage to transition to SendingEmail state
        var operationSucceededMessage = new OperationSucceededMessage()
        {
            CorrelationId = correlationId,
            OperationType = OperationType.UserProfileDelete
        };
        await harness.Bus.Publish(operationSucceededMessage);

        // Assert that the saga instance transitioned to Sending Email state due to the OperationSucceededMessage
        (await sagaHarness.Consumed.Any<OperationSucceededMessage>()).Should().BeTrue();

        instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("SendingEmail");

        // 3. Publish SendEmailMessage, which will fail in the FaultySendEmailConsumer
        var sendEmailMessage = new SendEmailMessage()
        {
            CorrelationId = correlationId,
            To = "test@mail.dk",
            Body = "test",
            Subject = "test"
        };
        await harness.Bus.Publish(sendEmailMessage);

        // Assert that the saga instance transitioned to Failed state due to the fault
        (await sagaHarness.Consumed.Any<Fault<SendEmailMessage>>()).Should().BeTrue();

        instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("Failed");
        await harness.Stop();
    }

    [Fact]
    public async Task UserDeleteSaga_Should_Finish_When_All_Events_Are_Handled()
    {
        // Arrange
        var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<UserDeleteSagaStateMachine, UserDeleteSagaState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var sagaHarness = harness.GetSagaStateMachineHarness<UserDeleteSagaStateMachine, UserDeleteSagaState>();
        var correlationId = Guid.NewGuid();

        // Act
        // 1. Publish UserDeletedMessage to create the saga instance and transition to DeletingUserProfile
        var userDeletedMessage = new UserDeletedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "test@mail.dk",
            CorrelationId = correlationId
        };
        await harness.Bus.Publish(userDeletedMessage);

        // Assert that the saga instance is created and in the DeletingUserProfile state
        (await sagaHarness.Consumed.Any<UserDeletedMessage>()).Should().BeTrue();

        var instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("DeletingUserProfile");

        // 2. Publish OperationSucceededMessage to transition to SendingEmail state
        var userProfileDeleteOperationSucceededMessage = new OperationSucceededMessage()
        {
            CorrelationId = correlationId,
            OperationType = OperationType.UserProfileDelete
        };
        await harness.Bus.Publish(userProfileDeleteOperationSucceededMessage);

        // Assert that the saga instance transitioned to SendingEmail state due to the OperationSucceededMessage
        (await sagaHarness.Consumed.Any<OperationSucceededMessage>()).Should().BeTrue();

        instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("SendingEmail");

        // 3. Publish OperationSucceededMessage for SendEmail to transition to Finished state
        var emailOperationSucceededMessage =  new OperationSucceededMessage()
        {
            CorrelationId = correlationId,
            OperationType = OperationType.SendEmail
        };
        await harness.Bus.Publish(emailOperationSucceededMessage);

        // Assert that the saga instance transitioned to Finished state due to the OperationSucceededMessage
        (await sagaHarness.Consumed.Any<OperationSucceededMessage>(x => x.Context.Message.OperationType == OperationType.SendEmail)).Should().BeTrue();

        instance = sagaHarness.Created.Contains(correlationId);
        instance.Should().NotBeNull();
        instance.CurrentState.Should().Be("Final");
        await harness.Stop();
    }
}

public class FaultyUserProfileDeleteConsumer : IConsumer<UserDeleteMessage>
{
    public Task Consume(ConsumeContext<UserDeleteMessage> context)
    {
        throw new Exception("Simulated failure during UserProfileDelete");
    }
}

public class FaultySendEmailConsumer : IConsumer<SendEmailMessage>
{
    public Task Consume(ConsumeContext<SendEmailMessage> context)
    {
        throw new Exception("Simulated failure during SendEmail");
    }
}
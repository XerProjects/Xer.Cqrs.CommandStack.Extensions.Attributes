using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xer.Cqrs.CommandStack;
using Xer.Cqrs.CommandStack.Extensions.Attributes.Tests.Entities;
using Xer.Delegator;
using Xer.Delegator.Registration;
using Xunit;
using Xunit.Abstractions;

namespace Xer.Cqrs.CommandStack.Extensions.Attributes.Tests.Registration
{
    public class AttributeRegistrationTests
    {
        #region RegisterCommandHandlersByAttribute Method

        public class RegisterCommandHandlerAttributes
        {
            private readonly ITestOutputHelper _outputHelper;

            public RegisterCommandHandlerAttributes(ITestOutputHelper outputHelper)
            {
                _outputHelper = outputHelper;
            }

            [Fact]
            public async Task ShouldRegisterAllMethodsOfTypeThatIsMarkedWithCommandHandlerAttribute()
            {
                var commandHandler = new TestAttributedCommandHandler(_outputHelper);

                var registration = new SingleMessageHandlerRegistration();
                registration.RegisterCommandHandlersByAttribute(() => commandHandler);

                IMessageHandlerResolver resolver = registration.BuildMessageHandlerResolver();

                MessageHandlerDelegate commandHandlerDelegate = resolver.ResolveMessageHandler(typeof(TestCommand));

                commandHandlerDelegate.Should().NotBeNull();

                // Delegate should invoke the actual command handler - TestAttributedCommandHandler.
                await commandHandlerDelegate.Invoke(new TestCommand());

                commandHandler.HandledCommands.Should().HaveCount(1);
                commandHandler.HasHandledCommand<TestCommand>().Should().BeTrue();
            }

            [Fact]
            public async Task ShouldRegisterAllCommandHandlerAttributeMethods()
            {
                var commandHandler = new TestAttributedCommandHandler(_outputHelper);

                // Get methods marked with [CommandHandler] attribute.
                IEnumerable<CommandHandlerAttributeMethod> methods = CommandHandlerAttributeMethod.FromType(() => commandHandler);

                var registration = new SingleMessageHandlerRegistration();
                registration.RegisterCommandHandlersByAttribute(methods);

                IMessageHandlerResolver resolver = registration.BuildMessageHandlerResolver();

                MessageHandlerDelegate commandHandlerDelegate = resolver.ResolveMessageHandler(typeof(TestCommand));

                commandHandlerDelegate.Should().NotBeNull();

                // Delegate should invoke the actual command handler - TestAttributedCommandHandler.
                await commandHandlerDelegate.Invoke(new TestCommand());

                commandHandler.HandledCommands.Should().HaveCount(1);
                commandHandler.HasHandledCommand<TestCommand>().Should().BeTrue();
            }

            [Fact]
            public void ShouldNotAllowSyncMethodsWithCancellationToken()
            {
                Action action = () =>
                {
                    try
                    {
                        var registration = new SingleMessageHandlerRegistration();
                        registration.RegisterCommandHandlersByAttribute(() => new TestAttributedSyncCommandHandlerWithCancellationToken(_outputHelper));
                    }
                    catch (Exception ex)
                    {
                        _outputHelper.WriteLine(ex.ToString());
                        throw;
                    }
                };

                action.Should().Throw<InvalidOperationException>();
            }
        }

        #endregion RegisterCommandHandlersByAttribute Method
    }
}

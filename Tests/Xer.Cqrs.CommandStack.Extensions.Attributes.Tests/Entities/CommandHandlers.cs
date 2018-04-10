using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xer.Cqrs.CommandStack;
using Xer.Cqrs.CommandStack.Extensions.Attributes;
using Xunit.Abstractions;

namespace Xer.Cqrs.CommandStack.Extensions.Attributes.Tests.Entities
{
    #region Base Command Handler

    public class TestCommandHandler
    {
        protected List<object> InternalHandledCommands { get; } = new List<object>();

        protected ITestOutputHelper OutputHelper { get; }

        public IReadOnlyCollection<object> HandledCommands => InternalHandledCommands.AsReadOnly();

        public TestCommandHandler(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        public bool HasHandledCommand<TCommand>()
        {
            return InternalHandledCommands.Any(c => c is TCommand);
        }

        protected virtual void BaseHandle<TCommand>(TCommand command) where TCommand : class
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            OutputHelper.WriteLine($"{DateTime.Now}: {GetType().Name} executed command of type {command.GetType().Name}.");
            InternalHandledCommands.Add(command);
        }
    }

    #endregion Base Command Handler

    #region Attributed Command Handlers

    public class TestAttributedCommandHandler : TestCommandHandler
    {

        public TestAttributedCommandHandler(ITestOutputHelper output)
            : base(output)
        {
        }

        [CommandHandler]
        public void HandleTestCommand(TestCommand command)
        {
            BaseHandle(command);
        }

        [CommandHandler]
        public void HandleThrowExceptionCommand(ThrowExceptionCommand command)
        {
            BaseHandle(command);
            throw new TestCommandHandlerException("This is a triggered exception.");
        }

        [CommandHandler]
        public Task HandleCancellableTestCommand(CancellableTestCommand command, CancellationToken ctx)
        {
            if(ctx == null)
            {
                return Task.FromException(new TestCommandHandlerException("Cancellation token is null. Please check attribute registration."));
            }

            BaseHandle(command);
            return Task.CompletedTask;
        }

        [CommandHandler]
        public Task HandleNonCancellableTestCommand(NonCancellableTestCommand command)
        {
            BaseHandle(command);
            return Task.CompletedTask;
        }

        [CommandHandler]
        public async Task HandleDelayCommand(DelayCommand command, CancellationToken ctx)
        {
            await Task.Delay(command.DurationInMilliSeconds, ctx);

            BaseHandle(command);
        }
    }

    /// <summary>
    /// This will not be allowed.
    /// </summary>
    public class TestAttributedSyncCommandHandlerWithCancellationToken : TestCommandHandler
    {
        public TestAttributedSyncCommandHandlerWithCancellationToken(ITestOutputHelper output)
            : base(output)
        {
        }

        [CommandHandler]
        public void HandleTestCommand(TestCommand command, CancellationToken cancellationToken)
        {
            BaseHandle(command);
        }
    }

    #endregion Attributed Command Handlers
    
    public class TestCommandHandlerException : Exception
    {
        public TestCommandHandlerException() { }
        public TestCommandHandlerException(string message) : base(message) { }
    }
}

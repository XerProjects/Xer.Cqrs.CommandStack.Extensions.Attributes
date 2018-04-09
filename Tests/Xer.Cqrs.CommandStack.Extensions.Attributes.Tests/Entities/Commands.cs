using Xer.Cqrs.CommandStack;

namespace Xer.Cqrs.CommandStack.Extensions.Attributes.Tests.Entities
{
    public class TestCommand
    {

    }

    public class ThrowExceptionCommand
    {

    }

    public class CancellableTestCommand
    {

    }

    public class NonCancellableTestCommand
    {

    }

    public class DelayCommand
    {
        public int DurationInMilliSeconds { get; }

        public DelayCommand(int milliSeconds)
        {
            DurationInMilliSeconds = milliSeconds;
        }
    }
}

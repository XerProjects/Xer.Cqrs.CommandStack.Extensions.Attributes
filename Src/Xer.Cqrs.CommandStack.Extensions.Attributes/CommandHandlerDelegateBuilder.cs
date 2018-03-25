using System;
using System.Threading;
using System.Threading.Tasks;
using Xer.Delegator;

namespace Xer.Cqrs.CommandStack.Extensions.Attributes
{
    internal class CommandHandlerDelegateBuilder
    {
        #region From Delegate
        
        internal static Func<TCommand, CancellationToken, Task> FromDelegate<TAttributed, TCommand>(Func<object> attributedObjectFactory, 
                                                                                                    Func<TAttributed, TCommand, Task> nonCancellableAsyncDelegate)
                                                                                                    where TAttributed : class
                                                                                                    where TCommand : class
        {
            if (attributedObjectFactory == null)
            {
                throw new ArgumentNullException(nameof(attributedObjectFactory));
            }

            if (nonCancellableAsyncDelegate == null)
            {
                throw new ArgumentNullException(nameof(nonCancellableAsyncDelegate));
            }

            return (inputCommand, cancellationToken) =>
            {
                if (!TryGetExpectedInstanceFromFactory(attributedObjectFactory, out TAttributed instance, out Exception exception))
                {
                    // Exception occurred or null is returned by factory.
                    return TaskUtility.FromException(exception);
                }
                
                return nonCancellableAsyncDelegate.Invoke(instance, inputCommand);
            };
        }

        internal static Func<TCommand, CancellationToken, Task> FromDelegate<TAttributed, TCommand>(Func<object> attributedObjectFactory, 
                                                                                                    Func<TAttributed, TCommand, CancellationToken, Task> cancellableAsyncDelegate)
                                                                                                    where TAttributed : class
                                                                                                    where TCommand : class
        {
            if (attributedObjectFactory == null)
            {
                throw new ArgumentNullException(nameof(attributedObjectFactory));
            }

            if (cancellableAsyncDelegate == null)
            {
                throw new ArgumentNullException(nameof(cancellableAsyncDelegate));
            }

            return (inputCommand, cancellationToken) =>
            {
                if (!TryGetExpectedInstanceFromFactory(attributedObjectFactory, out TAttributed instance, out Exception exception))
                {
                    // Exception occurred or null is returned by factory.
                    return TaskUtility.FromException(exception);
                }
                
                return cancellableAsyncDelegate.Invoke(instance, inputCommand, cancellationToken);
            };
        }

        internal static Func<TCommand, CancellationToken, Task> FromDelegate<TAttributed, TCommand>(Func<object> attributedObjectFactory, 
                                                                                                    Action<TAttributed, TCommand> action)
                                                                                                    where TAttributed : class
                                                                                                    where TCommand : class
        {
            if (attributedObjectFactory == null)
            {
                throw new ArgumentNullException(nameof(attributedObjectFactory));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return (inputCommand, cancellationToken) =>
            {
                try
                {
                    if (!TryGetExpectedInstanceFromFactory(attributedObjectFactory, out TAttributed instance, out Exception exception))
                    {
                        // Exception occurred or null is returned by factory.
                        return TaskUtility.FromException(exception);
                    }

                    action.Invoke(instance, inputCommand);
                    return TaskUtility.CompletedTask;
                }
                catch (Exception ex)
                {
                    return TaskUtility.FromException(ex);
                }
            };
        }

        #endregion From Delegate

        #region Functions

        private static bool TryGetExpectedInstanceFromFactory<TExpectedInstance>(Func<object> factory, out TExpectedInstance instance, out Exception exception) 
            where TExpectedInstance : class
        {            
            // Defaults.
            instance = null;

            if(TryGetInstanceFromFactory(factory, out var factoryInstance, out exception))
            {
                instance = factoryInstance as TExpectedInstance;
                if (instance == null)
                {
                    exception = InvalidInstanceFromFactoryDelegateException(typeof(TExpectedInstance), factoryInstance.GetType());
                    return false;
                }

                return true;
            }

            return false;
        }

        private static bool TryGetInstanceFromFactory<T>(Func<T> factory, out T instance, out Exception exception) 
            where T : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            
            // Defaults.
            instance = null;
            exception = null;

            try
            {
                instance = factory.Invoke();
                if (instance == null)
                {
                    // Factory returned null, no exception actually occurred.
                    exception = FailedToRetrieveInstanceFromFactoryDelegateException<T>();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Wrap inner exception.
                exception = FailedToRetrieveInstanceFromFactoryDelegateException<T>(ex);
                return false;
            }
        }
        
        private static InvalidOperationException FailedToRetrieveInstanceFromFactoryDelegateException<TInstance>(Exception ex = null)
        {
            return new InvalidOperationException($"Failed to retrieve an instance of {typeof(TInstance).Name} from the instance factory delegate. Please check registration configuration.", ex);
        }

        private static InvalidOperationException InvalidInstanceFromFactoryDelegateException(Type expected, Type actual, Exception ex = null)
        {
            return new InvalidOperationException($"Invalid instance provided by factory delegate. Expected instnece is of {expected.Name} but was given {actual.Name}.", ex);
        }

        #endregion Functions
    }
}

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xer.Delegator;

namespace Xer.Cqrs.CommandStack.Extensions.Attributes
{
    /// <summary>
    /// Represents a single method that is marked with an [CommandHandler] attribute.
    /// <para>Supported signatures for methods marked with [CommandHandler] are: (Methods can be named differently)</para>
    /// <para>- void HandleCommand(TCommand command);</para>
    /// <para>- Task HandleCommandAsync(TCommand command);</para>
    /// <para>- Task HandleCommandAsync(TCommand command, CancellationToken cancellationToken);</para>
    /// </summary>
    public partial class CommandHandlerAttributeMethod
    {
        #region Static Declarations
        
        private static readonly MethodInfo BuildWrappedSyncDelegateOpenGenericMethodInfo = typeof(CommandHandlerAttributeMethod).GetTypeInfo().GetDeclaredMethod(nameof(BuildWrappedSyncDelegate));
        private static readonly MethodInfo BuildCancellableAsyncDelegateOpenGenericMethodInfo = typeof(CommandHandlerAttributeMethod).GetTypeInfo().GetDeclaredMethod(nameof(BuildCancellableAsyncDelegate));
        private static readonly MethodInfo BuildNonCancellableAsyncDelegateOpenGenericMethodInfo = typeof(CommandHandlerAttributeMethod).GetTypeInfo().GetDeclaredMethod(nameof(BuildNonCancellableAsyncDelegate));
        
        #endregion Static Declarations

        #region Properties

        /// <summary>
        /// Method's declaring type.
        /// </summary>
        /// <returns></returns>
        public Type DeclaringType { get; }

        /// <summary>
        /// Factory delegate that provides an instance of this method's declaring type.
        /// </summary>
        public Func<object> InstanceFactory { get; }

        /// <summary>
        /// Type of command handled by the method.
        /// </summary>
        /// <returns></returns>
        public Type CommandType { get; }
        
        /// <summary>
        /// Method info.
        /// </summary>
        public MethodInfo MethodInfo { get; }

        /// <summary>
        /// Indicates if method is an asynchronous method.
        /// </summary>
        public bool IsAsync { get; }

        /// <summary>
        /// Indicates if method supports cancellation.
        /// </summary>
        public bool SupportsCancellation { get; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="methodInfo">Method info.</param>
        /// <param name="commandType">Type of command that is accepted by this method.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of the method info's declaring type.</param>
        /// <param name="isAsync">Is method an async method?</param>
        /// <param name="supportsCancellation">Does method supports cancellation?</param>
        private CommandHandlerAttributeMethod(MethodInfo methodInfo, Type commandType, Func<object> instanceFactory, bool isAsync, bool supportsCancellation)
        {
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            DeclaringType = methodInfo.DeclaringType;
            CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
            InstanceFactory = instanceFactory;
            IsAsync = isAsync;
            SupportsCancellation = supportsCancellation;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Create a delegate that handles a command that is specified in <see cref="Xer.Delegator.Registrations.CommandHandlerAttributeMethod.CommandType"/>.
        /// </summary>
        /// <returns>Delegate that handles a command that is specified in <see cref="Xer.Delegator.Registrations.CommandHandlerAttributeMethod.CommandType"/>.</returns>
        public MessageHandlerDelegate CreateCommandHandlerDelegate()
        {
            try
            {
                if (IsAsync)
                {
                    if (SupportsCancellation)
                    {
                        // Invoke BuildCancellableAsyncDelegate<TDeclaringType, TCommand>()
                        return InvokeDelegateBuilderMethod(BuildCancellableAsyncDelegateOpenGenericMethodInfo);
                    }
                    else
                    {
                        
                        // Invoke BuildNonCancellableAsyncDelegate<TDeclaringType, TCommand>()
                        return InvokeDelegateBuilderMethod(BuildNonCancellableAsyncDelegateOpenGenericMethodInfo);
                    }
                }
                else
                {
                    // Invoke BuildWrappedSyncDelegate<TDeclaringType, TCommand>()
                    return InvokeDelegateBuilderMethod(BuildWrappedSyncDelegateOpenGenericMethodInfo);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create command handler delegate for {DeclaringType.Name}'s {MethodInfo.ToString()} method.", ex);
            }
        }        

        /// <summary>
        /// Check if a method marked with [CommandHandler] attribute is found in the specified type.
        /// </summary>
        /// <param name="type">Type to search for methods marked with [CommandHandler] attribute.</param>
        /// <returns>True if atleast on method is found. Otherwise, false.</returns>
        public static bool IsFoundInType(Type type)
        {
            return IsFoundInType(type.GetTypeInfo());
        }

        /// <summary>
        /// Check if a method marked with [CommandHandler] attribute is found in the specified type.
        /// </summary>
        /// <param name="typeInfo">Type to search for methods marked with [CommandHandler] attribute.</param>
        /// <returns>True if atleast on method is found. Otherwise, false.</returns>
        public static bool IsFoundInType(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return typeInfo.DeclaredMethods.Any(method => IsValid(method));
        }

        /// <summary>
        /// Check if method is marked with [CommandHandler] attribute.
        /// </summary>
        /// <param name="methodInfo">Method to search for a [CommandHandler] attribute.</param>
        /// <returns>True if attribute is found. Otherwise, false.</returns>
        public static bool IsValid(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            return methodInfo.GetCustomAttributes(typeof(CommandHandlerAttribute), true).Any();
        }

        #endregion Methods

        #region Functions

        /// <summary>
        /// Create a delegate from an asynchronous (cancellable) action.
        /// </summary>
        /// <typeparam name="TAttributed">Type that contains [CommandHandler] methods. This should match DeclaringType property.</typeparam>
        /// <typeparam name="TCommand">Type of command that is handled by the CommandHandlerAttributeMethod. This should match CommandType property.</typeparam>
        /// <returns>Delegate that handles a command.</returns>
        private MessageHandlerDelegate BuildCancellableAsyncDelegate<TAttributed, TCommand>() 
            where TAttributed : class
            where TCommand : class
        {
            // Create a delegate from method info. First argument is the target object.
            Func<TAttributed, TCommand, CancellationToken, Task> cancellableAsyncDelegate = (Func<TAttributed, TCommand, CancellationToken, Task>)
                MethodInfo.CreateDelegate(typeof(Func<TAttributed, TCommand, CancellationToken, Task>));

            return CommandHandlerDelegateBuilder.FromDelegate(InstanceFactory, cancellableAsyncDelegate);
        }

        /// <summary>
        /// Create a delegate from an asynchronous (non-cancellable) action.
        /// </summary>
        /// <typeparam name="TAttributed">Type that contains [CommandHandler] methods. This should match DeclaringType property.</typeparam>
        /// <typeparam name="TCommand">Type of command that is handled by the CommandHandlerAttributeMethod. This should match CommandType property.</typeparam>
        /// <returns>Delegate that handles a command.</returns>
        private MessageHandlerDelegate BuildNonCancellableAsyncDelegate<TAttributed, TCommand>() 
            where TAttributed : class
            where TCommand : class
        {
            // Create a delegate from method info. First argument is the target object.
            Func<TAttributed, TCommand, Task> nonCancellableAsyncDelegate = (Func<TAttributed, TCommand, Task>)
                MethodInfo.CreateDelegate(typeof(Func<TAttributed, TCommand, Task>));

            return CommandHandlerDelegateBuilder.FromDelegate(InstanceFactory, nonCancellableAsyncDelegate);
        }

        /// <summary>
        /// Create a delegate from a synchronous action.
        /// </summary>
        /// <typeparam name="TAttributed">Type that contains [CommandHandler] methods. This should match DeclaringType property.</typeparam>
        /// <typeparam name="TCommand">Type of command that is handled by the CommandHandlerAttributeMethod. This should match CommandType property.</typeparam>
        /// <returns>Delegate that handles a command.</returns>
        private MessageHandlerDelegate BuildWrappedSyncDelegate<TAttributed, TCommand>() 
            where TAttributed : class
            where TCommand : class
        {
            // Create a delegate from method info. First argument is the target object.
            Action<TAttributed, TCommand> action = (Action<TAttributed, TCommand>)
                MethodInfo.CreateDelegate(typeof(Action<TAttributed, TCommand>));

            return CommandHandlerDelegateBuilder.FromDelegate(InstanceFactory, action);
        }

        /// <summary>
        /// Invoke the specified method to build a delegate that can handle this CommandHandlerAttributeMethod's command type.
        /// </summary>
        /// <param name="openGenericBuildDelegateMethodInfo">Method to invoke.</param>
        /// <returns>Delegate that can handle this CommandHandlerAttributeMethod's command type.</returns>
        private MessageHandlerDelegate InvokeDelegateBuilderMethod(MethodInfo openGenericBuildDelegateMethodInfo)
        {
            return (MessageHandlerDelegate)openGenericBuildDelegateMethodInfo
                .MakeGenericMethod(DeclaringType, CommandType)
                .Invoke(this, new object[] { /* No arguments */ });
        }

        #endregion Functions
    }
}

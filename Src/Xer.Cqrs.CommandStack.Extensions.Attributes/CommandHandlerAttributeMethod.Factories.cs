using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Xer.Cqrs.CommandStack.Extensions.Attributes
{
    public partial class CommandHandlerAttributeMethod
    {
        #region Factory Methods
        
        /// <summary>
        /// Create CommandHandlerAttributeMethod from the method info.
        /// </summary>
        /// <param name="methodInfo">Method info that has CommandHandlerAttribute custom attribute.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of the method info's declaring type.</param>
        /// <returns>Instance of CommandHandlerAttributeMethod.</returns>
        public static CommandHandlerAttributeMethod FromMethodInfo(MethodInfo methodInfo, Func<object> instanceFactory)
        {
            Type commandType;
            bool isAsyncMethod;

            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (!IsValid(methodInfo))
            {
                throw new InvalidOperationException($"Method is not marked with [CommandHandler] attribute. {createCheckMethodMessage(methodInfo)}.");
            }

            // Get all method parameters.
            ParameterInfo[] methodParameters = methodInfo.GetParameters();

            // Get first method parameter that is a class (not struct). This assumes that the first parameter is the command.
            ParameterInfo commandParameter = methodParameters.FirstOrDefault();
            if (commandParameter != null)
            {
                // Check if parameter is a class.
                if (!commandParameter.ParameterType.GetTypeInfo().IsClass)
                {
                    throw new InvalidOperationException($"Method's command parameter is not a reference type, only reference type commands are supported. {createCheckMethodMessage(methodInfo)}.");
                }
                           
                // Set command type.
                commandType = commandParameter.ParameterType;
            }
            else
            {                
                // Method has no parameter.
                throw new InvalidOperationException($"Method must accept a command object as a parameter. {createCheckMethodMessage(methodInfo)}.");
            }

            // Only valid return types are Task/void.
            if (methodInfo.ReturnType == typeof(Task))
            {
                isAsyncMethod = true;
            }
            else if (methodInfo.ReturnType == typeof(void))
            {
                isAsyncMethod = false;

                // if(methodInfo.CustomAttributes.Any(p => p.AttributeType == typeof(AsyncStateMachineAttribute)))
                // {
                //     throw new InvalidOperationException($"Methods with async void signatures are not allowed. A Task may be used as return type instead of void. Check method: {methodInfo.ToString()}.");
                // }
            }
            else
            {
                // Return type is not Task/void. Invalid.
                throw new InvalidOperationException($"Method marked with [CommandHandler] can only have void or a Task as return value. {createCheckMethodMessage(methodInfo)}.");
            }

            bool supportsCancellation = methodParameters.Any(p => p.ParameterType == typeof(CancellationToken));

            if (!isAsyncMethod && supportsCancellation)
            {
                throw new InvalidOperationException($"Cancellation token support is only available for async methods (methods returning a Task). {createCheckMethodMessage(methodInfo)}.");
            }

            return new CommandHandlerAttributeMethod(methodInfo, commandType, instanceFactory, isAsyncMethod, supportsCancellation);

            // Local function.
            string createCheckMethodMessage(MethodInfo method) => $"Check {methodInfo.DeclaringType.Name}'s {methodInfo.ToString()} method";
        }

        /// <summary>
        /// Create CommandHandlerAttributeMethod from the method info.
        /// </summary>
        /// <param name="methodInfos">Method infos that have CommandHandlerAttribute custom attributes.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of a method info's declaring type.</param>
        /// <returns>Instances of CommandHandlerAttributeMethod.</returns>
        public static IEnumerable<CommandHandlerAttributeMethod> FromMethodInfos(IEnumerable<MethodInfo> methodInfos, Func<Type, object> instanceFactory)
        {
            if (methodInfos == null)
            {
                throw new ArgumentNullException(nameof(methodInfos));
            }

            return methodInfos.Select(m => FromMethodInfo(m, () => instanceFactory.Invoke(m.DeclaringType)));
        }

        /// <summary>
        /// Detect methods marked with [CommandHandler] attribute and translate to CommandHandlerAttributeMethod instances.
        /// </summary>
        /// <typeparam name="T">Type to scan for methods marked with the [CommandHandler] attribute.</typeparam>
        /// <param name="instanceFactory">Factory delegate that provides an instance of the specified type.</param>
        /// <returns>List of all CommandHandlerAttributeMethod detected.</returns>
        public static IEnumerable<CommandHandlerAttributeMethod> FromType<T>(Func<T> instanceFactory) where T : class
        {
            return FromType(typeof(T), instanceFactory);
        }

        /// <summary>
        /// Detect methods marked with [CommandHandler] attribute and translate to CommandHandlerAttributeMethod instances.
        /// </summary>
        /// <param name="type">Type to scan for methods marked with the [CommandHandler] attribute.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of the specified type.</param>
        /// <returns>List of all CommandHandlerAttributeMethod detected.</returns>
        public static IEnumerable<CommandHandlerAttributeMethod> FromType(Type type, Func<object> instanceFactory)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            IEnumerable<MethodInfo> methods = type.GetTypeInfo().DeclaredMethods.Where(m => IsValid(m));

            return FromMethodInfos(methods, _ => instanceFactory.Invoke());
        }

        /// <summary>
        /// Detect methods marked with [CommandHandler] attribute and translate to CommandHandlerAttributeMethod instances.
        /// </summary>
        /// <param name="types">Types to scan for methods marked with the [CommandHandler] attribute.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of a given type.</param>
        /// <returns>List of all CommandHandlerAttributeMethod detected.</returns>
        public static IEnumerable<CommandHandlerAttributeMethod> FromTypes(IEnumerable<Type> types, Func<Type, object> instanceFactory)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return types.SelectMany(type => FromType(type, () => instanceFactory.Invoke(type)));
        }

        /// <summary>
        /// Detect methods marked with [CommandHandler] attribute and translate to CommandHandlerAttributeMethod instances.
        /// </summary>
        /// <param name="commandHandlerAssembly">Assembly to scan for methods marked with the [CommandHandler] attribute.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of a type that has methods marked with [CommandHandler] attribute.</param>
        /// <returns>List of all CommandHandlerAttributeMethod detected.</returns>
        public static IEnumerable<CommandHandlerAttributeMethod> FromAssembly(Assembly commandHandlerAssembly, Func<Type, object> instanceFactory)
        {
            if (commandHandlerAssembly == null)
            {
                throw new ArgumentNullException(nameof(commandHandlerAssembly));
            }

            IEnumerable<MethodInfo> commandHandlerMethods = commandHandlerAssembly.DefinedTypes
                                                                .Where(typeInfo => IsFoundInType(typeInfo))
                                                                .SelectMany(typeInfo => typeInfo.DeclaredMethods.Where(method => 
                                                                    IsValid(method)));
            
            return FromMethodInfos(commandHandlerMethods, instanceFactory);
        }

        /// <summary>
        /// Detect methods marked with [CommandHandler] attribute and translate to CommandHandlerAttributeMethod instances.
        /// </summary>
        /// <param name="commandHandlerAssemblies">Assemblies to scan for methods marked with the [CommandHandler] attribute.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of a type that has methods marked with [CommandHandler] attribute.</param>
        /// <returns>List of all CommandHandlerAttributeMethod detected.</returns>
        public static IEnumerable<CommandHandlerAttributeMethod> FromAssemblies(IEnumerable<Assembly> commandHandlerAssemblies, Func<Type, object> instanceFactory)
        {
            if (commandHandlerAssemblies == null)
            {
                throw new ArgumentNullException(nameof(commandHandlerAssemblies));
            }

            return commandHandlerAssemblies.SelectMany(assembly => FromAssembly(assembly, instanceFactory));
        }
        
        #endregion Factory Methods
    }
}
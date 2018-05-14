# Xer.Cqrs.CommandStack.Extensions.Attributes
Attribute registration extension for Xer.Cqrs.CommandStack

# Build
| Branch | Status |
|--------|--------|
| Master | [![Build status](https://ci.appveyor.com/api/projects/status/j6omv8ir8whiraya?svg=true)](https://ci.appveyor.com/project/XerProjects25246/xer-cqrs-commandstack-extensions-attributes) |
| Dev | [![Build status](https://ci.appveyor.com/api/projects/status/j6omv8ir8whiraya/branch/dev?svg=true)](https://ci.appveyor.com/project/XerProjects25246/xer-cqrs-commandstack-extensions-attributes/branch/dev) |

# Nuget
[![NuGet](https://img.shields.io/nuget/vpre/xer.cqrs.commandstack.extensions.attributes.svg)](https://www.nuget.org/packages/Xer.Cqrs.CommandStack.Extensions.Attributes/)


## Installation
You can simply clone this repository, build the source, reference the dll from the project, and code away!

Xer.Cqrs.CommandStack.Extensions.Attributes library is available as a Nuget package: 

[![NuGet](https://img.shields.io/nuget/v/Xer.Cqrs.CommandStack.Extensions.Attributes.svg)](https://www.nuget.org/packages/Xer.Cqrs.CommandStack.Extensions.Attributes/)

To install Nuget packages:
1. Open command prompt
2. Go to project directory
3. Add the packages to the project:
    ```csharp
    dotnet add package Xer.Cqrs.CommandStack.Extensions.Attributes
    ```
4. Restore the packages:
    ```csharp
    dotnet restore
    ```

### Command Handler Attribute Registration

```csharp
// Example command.
public class RegisterProductCommand
{
    public int ProductId { get; }
    public string ProductName { get; }

    public RegisterProductCommand(int productId, string productName) 
    {
        ProductId = productId;
        ProductName = productName;
    }
}

// Example Command handler.
public class RegisterProductCommandHandler : ICommandAsyncHandler<RegisterProductCommand>
{
    private readonly IProductRepository _productRepository;

    public RegisterProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    [CommandHandler] // This is in Xer.Cqrs.CommandStack.Extensions.Attributes. This allows the method to registered as a command handler through attribute registration.
    public Task HandleAsync(RegisterProductCommand command, CancellationToken cancellationToken = default(CancellationToken))
    {
        return _productRepository.SaveAsync(new Product(command.ProductId, command.ProductName));
    }
}
```

```csharp
// Command Handler Registration
public CommandDelegator RegisterCommandHandlers()
{            
    // SingleMessageHandlerRegistration only allows registration of a single message handler per message type.
    var registration = new SingleMessageHandlerRegistration();
    
    // Register methods with [CommandHandler] attribute.
    registration.RegisterCommandHandlerAttributes(() => new RegisterProductCommandHandler(new ProductRepository()));

    // Build command delegator.
    IMessageHandlerResolver resolver = registration.BuildMessageHandlerResolver();
    return new CommandDelegator(resolver);
}
```

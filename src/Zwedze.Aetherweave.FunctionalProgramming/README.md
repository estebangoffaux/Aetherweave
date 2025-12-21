# Aetherweave.FunctionalProgramming

Railway-oriented programming and functional patterns for building robust, composable .NET applications.

## Features

- 🚂 **Railway-Oriented Programming** - Chain operations that can fail elegantly
- 🎯 **Type-Safe Error Handling** - No more exception-based control flow
- 🔗 **Composable Operations** - Map, Bind, and chain transformations
- ⚡ **Async Support** - First-class async/await integration
- 🛡️ **Compile-Time Safety** - Compiler ensures you handle both success and failure

## Installation

```bash
dotnet add package Zwedze.Aetherweave.FunctionalProgramming
```

## Core Concepts

### Result<T> - The Foundation

`Result<T>` represents an operation that can either succeed with a value of type `T` or fail with an `Error`.

```csharp
// Success case
Result<int> success = Result<int>.Success(42);

// Failure case
Result<int> failure = Result<int>.Fail(new Error("ERR_001", "Something went wrong"));
```

### Pattern Matching

```csharp
var result = DivideNumbers(10, 2);

var message = result.Match(
    onSuccess: value => $"Result: {value}",
    onFailure: error => $"Error: {error.Message}"
);
// message = "Result: 5"
```

## Quick Start

### Basic Usage

```csharp
public Result<int> ParseInteger(string input)
{
    if (int.TryParse(input, out var value))
    {
        return Result<int>.Success(value);
    }
    
    return Result<int>.Fail(new Error("PARSE_ERROR", $"'{input}' is not a valid integer"));
}

// Usage
var result = ParseInteger("123");

result.Match(
    onSuccess: value => Console.WriteLine($"Parsed: {value}"),
    onFailure: error => Console.WriteLine($"Failed: {error.Message}")
);
```

### Map - Transform Success Values

Use `Map` to transform the value inside a Result without changing success/failure state:

```csharp
Result<int>.Success(5)
    .Map(x => x * 2)           // Result<int>.Success(10)
    .Map(x => x.ToString())    // Result<string>.Success("10")
    .Map(x => $"Value: {x}")   // Result<string>.Success("Value: 10")
    .Match(
        onSuccess: Console.WriteLine,
        onFailure: error => Console.WriteLine(error.Message)
    );
```

**Key Point:** Map transforms the value but **cannot fail**. If the Result is already a failure, Map is skipped.

### Bind - Chain Operations That Can Fail

Use `Bind` to chain operations where each step can fail:

```csharp
public Result<int> ParseInt(string s)
{
    return int.TryParse(s, out var v) 
        ? Result<int>.Success(v) 
        : Result<int>.Fail(new Error("PARSE_ERROR", "Invalid integer"));
}

public Result<int> Divide(int numerator, int denominator)
{
    return denominator != 0
        ? Result<int>.Success(numerator / denominator)
        : Result<int>.Fail(new Error("DIV_ZERO", "Cannot divide by zero"));
}

// Chain operations - stops at first failure
var result = ParseInt("100")
    .Bind(value => Divide(value, 4))   // Result<int>.Success(25)
    .Bind(value => Divide(value, 5));  // Result<int>.Success(5)

// If any step fails, the rest are skipped
var failed = ParseInt("abc")           // Fails here
    .Bind(value => Divide(value, 4))   // Skipped
    .Bind(value => Divide(value, 5));  // Skipped
```

**Key Point:** Bind chains operations where each can return a new Result. The chain stops at the first failure.

## Map vs Bind - When to Use Which?

```csharp
// ✅ Use Map when transformation cannot fail
Result<int>.Success(5)
    .Map(x => x * 2)          // Simple math - cannot fail
    .Map(x => x.ToString())   // ToString - cannot fail

// ✅ Use Bind when operation can fail
Result<string>.Success("123")
    .Bind(s => ParseInt(s))        // Can fail - might not be a number
    .Bind(i => Divide(100, i))     // Can fail - division by zero
    .Map(result => $"Result: {result}")  // Simple formatting - cannot fail
```

## Railway-Oriented Programming Pattern

Build complex workflows by chaining operations:

```csharp
public class OrderService(
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IPaymentService paymentService)
{
    public async Task<Result<Order>> ProcessOrder(CreateOrderCommand command)
    {
        return await Result<CreateOrderCommand>.Success(command)
            .Bind(ValidateCommand)
            .BindAsync(CheckInventory)
            .BindAsync(CreateOrder)
            .BindAsync(ProcessPayment)
            .OnSuccess(order => logger.LogInformation("Order processed: {OrderId}", order.Id))
            .OnFailure(error => logger.LogError("Order failed: {Error}", error.Message));
    }

    private Result<CreateOrderCommand> ValidateCommand(CreateOrderCommand cmd)
    {
        if (!cmd.Items.Any())
            return Result<CreateOrderCommand>.Fail(
                new Error("EMPTY_ORDER", "Order must contain at least one item"));
            
        if (cmd.CustomerId <= 0)
            return Result<CreateOrderCommand>.Fail(
                new Error("INVALID_CUSTOMER", "Invalid customer ID"));
            
        return Result<CreateOrderCommand>.Success(cmd);
    }

    private async Task<Result<CreateOrderCommand>> CheckInventory(CreateOrderCommand cmd)
    {
        var available = await inventoryService.CheckAvailability(cmd.Items);
        
        return available
            ? Result<CreateOrderCommand>.Success(cmd)
            : Result<CreateOrderCommand>.Fail(
                new Error("INSUFFICIENT_STOCK", "Not enough inventory"));
    }

    private async Task<Result<Order>> CreateOrder(CreateOrderCommand cmd)
    {
        try
        {
            var order = new Order(cmd.CustomerId, cmd.Items);
            await orderRepository.AddAsync(order);
            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            return Result<Order>.Fail(
                new Error("DB_ERROR", $"Failed to create order: {ex.Message}"));
        }
    }

    private async Task<Result<Order>> ProcessPayment(Order order)
    {
        var paymentResult = await paymentService.ProcessAsync(order.Total);
        
        return paymentResult.IsSuccess
            ? Result<Order>.Success(order)
            : Result<Order>.Fail(
                new Error("PAYMENT_FAILED", "Payment processing failed"));
    }
}
```

## Side Effects with OnSuccess/OnFailure

Execute code for side effects without affecting the Result:

```csharp
Result<Order>.Success(order)
    .OnSuccess(o => logger.LogInformation("Order created: {OrderId}", o.Id))
    .OnSuccess(o => eventBus.Publish(new OrderCreatedEvent(o.Id)))
    .OnFailure(error => logger.LogError("Failed: {Error}", error.Message))
    .OnFailure(error => metrics.IncrementCounter("order.failures"));
```

These methods return the original Result, so you can continue chaining.

## Async Operations

### MapAsync

```csharp
public async Task<string> FormatOrderAsync(Order order)
{
    await Task.Delay(100); // Simulate async work
    return $"Order #{order.Id}";
}

var result = await Result<Order>.Success(order)
    .MapAsync(o => FormatOrderAsync(o));
```

### BindAsync

```csharp
public async Task<Result<Order>> GetOrderAsync(int orderId)
{
    var order = await orderRepository.GetByIdAsync(orderId);
    
    return order != null
        ? Result<Order>.Success(order)
        : Result<Order>.Fail(new Error("NOT_FOUND", "Order not found"));
}

var result = await Result<int>.Success(123)
    .BindAsync(id => GetOrderAsync(id))
    .BindAsync(order => ProcessPaymentAsync(order));
```

### MatchAsync

```csharp
var message = await result.MatchAsync(
    onSuccess: async order => 
    {
        await emailService.SendConfirmation(order);
        return "Order processed successfully";
    },
    onFailure: async error =>
    {
        await errorLogger.LogAsync(error);
        return $"Order failed: {error.Message}";
    }
);
```

## Integration with CQRS

### Command Handler

```csharp
public class CreateOrderHandler(
    IOrderRepository orderRepository,
    IUnitOfWorkFactory uowFactory) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<ResponseWrapper<Guid>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var result = await Result<CreateOrderCommand>.Success(request)
            .Bind(ValidateCommand)
            .BindAsync(async cmd =>
            {
                await using var uow = uowFactory.CreateTransactional();
                
                var order = new Order(cmd.CustomerId, cmd.Items);
                await orderRepository.AddAsync(order, cancellationToken);
                
                await uow.SaveChanges(cancellationToken);
                await uow.Commit(cancellationToken);
                
                return Result<Order>.Success(order);
            })
            .Map(order => order.Id);

        return result.Match(
            onSuccess: id => ResponseWrapper<Guid>.Ok(id),
            onFailure: error => ResponseWrapper<Guid>.Fail(
                ErrorFactory.Create(error.Code, error.Message))
        );
    }

    private Result<CreateOrderCommand> ValidateCommand(CreateOrderCommand cmd)
    {
        // Validation logic...
    }
}
```

## Advanced Patterns

### Combining Multiple Results

```csharp
public Result<decimal> CalculateTotal(Result<decimal> price, Result<int> quantity)
{
    return price.Bind(p =>
        quantity.Map(q => p * q));
}

// Usage
var price = Result<decimal>.Success(10.50m);
var quantity = Result<int>.Success(3);
var total = CalculateTotal(price, quantity);  // Result<decimal>.Success(31.50)
```

### Error Accumulation

```csharp
public class ValidationResult
{
    public List<Error> Errors { get; } = new();
    public bool IsValid => !Errors.Any();

    public Result<T> ToResult<T>(T value)
    {
        return IsValid
            ? Result<T>.Success(value)
            : Result<T>.Fail(Errors.First()); // Or combine errors
    }
}

public ValidationResult ValidateOrder(CreateOrderCommand cmd)
{
    var result = new ValidationResult();
    
    if (string.IsNullOrEmpty(cmd.CustomerName))
        result.Errors.Add(new Error("NAME_REQUIRED", "Customer name is required"));
        
    if (cmd.Items.Count == 0)
        result.Errors.Add(new Error("ITEMS_REQUIRED", "Order must have items"));
        
    if (cmd.Total <= 0)
        result.Errors.Add(new Error("INVALID_TOTAL", "Total must be greater than zero"));
        
    return result;
}
```

### Try Pattern

```csharp
public static Result<T> Try<T>(Func<T> operation, string errorCode = "OPERATION_FAILED")
{
    try
    {
        var result = operation();
        return Result<T>.Success(result);
    }
    catch (Exception ex)
    {
        return Result<T>.Fail(new Error(errorCode, ex.Message));
    }
}

// Usage
var result = Try(() => JsonSerializer.Deserialize<Order>(json), "JSON_PARSE_ERROR");
```

## Error Handling Best Practices

### ✅ DO

1. **Use specific error codes:**
   ```csharp
   new Error("CUSTOMER_NOT_FOUND", "Customer with ID 123 not found")
   new Error("INSUFFICIENT_BALANCE", "Account balance too low")
   ```

2. **Chain operations for readability:**
   ```csharp
   return await GetCustomer(customerId)
       .BindAsync(ValidateCustomer)
       .BindAsync(CreateOrder)
       .BindAsync(ProcessPayment);
   ```

3. **Use Map for transformations, Bind for operations that can fail:**
   ```csharp
   result.Map(order => order.Total)        // Cannot fail
   result.Bind(order => SaveOrder(order))  // Can fail
   ```

### ❌ DON'T

1. **Don't use exceptions for control flow:**
   ```csharp
   // Bad
   try { var order = GetOrder(); } catch { return null; }
   
   // Good
   Result<Order> GetOrder(int id) { ... }
   ```

2. **Don't mix Result with null returns:**
   ```csharp
   // Bad
   Result<Order>? GetOrder() => ...
   
   // Good
   Result<Order> GetOrder() => ...
   ```

3. **Don't ignore checking after an invocation returning a Result:**
   ```csharp
   // Bad
   var result = ProcessOrder(cmd);
   // ... never check if it succeeded
   
   // Good
   var result = ProcessOrder(cmd);
   result.Match(
       onSuccess: order => HandleSuccess(order),
       onFailure: error => HandleError(error)
   );
   ```

## API Reference

### Result<T>

| Method       | Signature                                                        | Description                    |
|--------------|------------------------------------------------------------------|--------------------------------|
| `Success`    | `Result<T> Success(T value)`                                     | Create successful result       |
| `Fail`       | `Result<T> Fail(Error error)`                                    | Create failed result           |
| `Match`      | `TResult Match<TResult>(Func<T, TResult>, Func<Error, TResult>)` | Pattern match on result        |
| `MatchAsync` | `Task<TResult> MatchAsync<TResult>(...)`                         | Async pattern matching         |
| `Map`        | `Result<TNext> Map<TNext>(Func<T, TNext>)`                       | Transform success value        |
| `MapAsync`   | `Task<Result<TNext>> MapAsync<TNext>(...)`                       | Async transform                |
| `Bind`       | `Result<TNext> Bind<TNext>(Func<T, Result<TNext>>)`              | Chain operations               |
| `BindAsync`  | `Task<Result<TNext>> BindAsync<TNext>(...)`                      | Async chain                    |
| `OnSuccess`  | `Result<T> OnSuccess(Action<T>)`                                 | Execute side effect on success |
| `OnFailure`  | `Result<T> OnFailure(Action<Error>)`                             | Execute side effect on failure |

### Error

| Property  | Type     | Description                          |
|-----------|----------|--------------------------------------|
| `Code`    | `string` | Error code for programmatic handling |
| `Message` | `string` | Human-readable error message         |

## Dependencies

- No external dependencies! Pure functional patterns.

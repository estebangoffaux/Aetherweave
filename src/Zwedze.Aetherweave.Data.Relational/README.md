# Aetherweave.Data.Relational

Clean, minimal EF Core wrapper with Unit of Work pattern for building maintainable data access layers.

## Features

- **Unit of Work Pattern** - Transactional unit of work with explicit commit/rollback
- **Auto-Configuration** - Configure DbContext from appsettings.json
- **Auto-Starting Transactions** - No manual BeginTransaction needed
- **Built-in Health Checks** - Ready for production monitoring
- **Provider Agnostic** - Works with any EF Core provider (PostgreSQL, SQL Server, SQLite, etc.)
- **Clean Disposal** - Auto-rollback uncommitted transactions with warnings

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Data.Relational
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL  # Or your preferred provider
```

## Two-Model Architecture

This library is designed for architectures that separate **domain models** from **persistence models**:

- **Domain model** (e.g. `Order`) - the public type used throughout the application and domain layers. Never referenced by EF Core directly.
- **Record model** (e.g. `OrderRecord`) - an `internal` type owned by the data layer. EF Core maps columns to this type. It is never exposed outside the data project.

`DbSet<T>` properties on your `DbContext` are typed against `XRecord` and marked `internal`, so they cannot leak persistence concerns into the application layer. Repositories are responsible for mapping between the two.

The `DbContext` itself is also an implementation detail: only repositories access it directly. Services and handlers depend exclusively on repository interfaces.

```
Application / domain layer  →  Order (domain model, public)
                                      ↕  IOrderRepository (public interface)
Data layer                  →  OrderRepository (internal implementation)
                                      ↕  maps via OrderMapper
                            →  OrderRecord (persistence model, internal)  →  EF Core / database
```

## Quick Start

### 1. Define Persistence Models, Repository Interface, and DbContext

**Persistence models** (internal to the data project):

```csharp
internal sealed record OrderRecord(long Id, string Code, long CustomerId, string Status);
internal sealed record OrderItemRecord(long Id, long OrderId, long ProductId, int Quantity);
```

**Repository interface** (public, lives in the application or domain layer):

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Id<Order> id, CancellationToken ct);
    Task<Order?> GetByCodeAsync(Code<Order> code, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
    void Update(Order order);
}
```

**DbContext** (scoped to the data project; `DbSet` properties are `internal`):

```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // Internal DbSets - persistence models only, not exposed to the application layer
    internal DbSet<OrderRecord> Orders => Set<OrderRecord>();
    internal DbSet<OrderItemRecord> OrderItems => Set<OrderItemRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

**Repository implementation** (internal to the data project; the only place `DbContext` is used):

```csharp
internal sealed class OrderRepository(ApplicationDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Id<Order> id, CancellationToken ct)
    {
        var record = await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == (long)id, ct);

        return record is null ? null : OrderMapper.ToDomain(record);
    }

    public async Task<Order?> GetByCodeAsync(Code<Order> code, CancellationToken ct)
    {
        var record = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Code == (string)code, ct);

        return record is null ? null : OrderMapper.ToDomain(record);
    }

    public async Task AddAsync(Order order, CancellationToken ct)
        => await dbContext.Orders.AddAsync(OrderMapper.ToRecord(order), ct);

    public void Update(Order order)
        => dbContext.Orders.Update(OrderMapper.ToRecord(order));
}
```

### 2. Configure in appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=myapp;Username=postgres;Password=secret"
  },
  "Aetherweave": {
    "DataRelational": {
      "ConnectionStringName": "DefaultConnection",
      "EnableDetailedErrors": true,
      "EnableSensitiveDataLogging": false,
      "NoTrackingAsDefaultTrackingStrategy": true
    }
  }
}
```

### 3. Register in Startup/Program.cs

**PostgreSQL:**

```csharp
services.AddAetherweaveData<ApplicationDbContext>(
    configuration,
    (builder, options) => builder.UseNpgsql(
        configuration.GetConnectionString(options.ConnectionStringName)));

services.AddScoped<IOrderRepository, OrderRepository>();
```

**SQL Server:**

```csharp
services.AddAetherweaveData<ApplicationDbContext>(
    configuration,
    (builder, options) => builder.UseSqlServer(
        configuration.GetConnectionString(options.ConnectionStringName)));
```

**SQLite:**

```csharp
services.AddAetherweaveData<ApplicationDbContext>(
    configuration,
    (builder, options) => builder.UseSqlite(
        configuration.GetConnectionString(options.ConnectionStringName)));
```

### 4. Use in Your Application

**Read-only query - inject the repository, not the DbContext:**

```csharp
public sealed class OrderQueryService(IOrderRepository orderRepository)
{
    public Task<Order?> GetOrderAsync(Id<Order> orderId, CancellationToken ct)
        => orderRepository.GetByIdAsync(orderId, ct);
}
```

**Transactional write - the service coordinates the UoW; the repository handles persistence:**

```csharp
public sealed class OrderCommandService(IUnitOfWorkFactory uowFactory, IOrderRepository orderRepository)
{
    public async Task CreateOrderAsync(CreateOrderCommand command, CancellationToken ct)
    {
        await using var uow = uowFactory.CreateTransactional();

        var order = Order.Create(command.CustomerId, command.Items);
        await orderRepository.AddAsync(order, ct);
        await uow.SaveChanges(ct);

        var invoice = Invoice.For(order);
        await invoiceRepository.AddAsync(invoice, ct);
        await uow.SaveChanges(ct);

        await uow.Commit(ct);
        // If you forget to commit, the transaction auto-rolls back on dispose
    }
}
```

## Configuration Options

### DataRelationalOptions

| Property                              | Type     | Default    | Description                                           |
|---------------------------------------|----------|------------|-------------------------------------------------------|
| `ConnectionStringName`                | `string` | *required* | Name of connection string in appsettings.json         |
| `EnableDetailedErrors`                | `bool`   | `true`     | Show detailed EF Core errors (disable in production)  |
| `EnableSensitiveDataLogging`          | `bool`   | `false`    | Log parameter values (security risk, use only in dev) |
| `NoTrackingAsDefaultTrackingStrategy` | `bool`   | `true`     | Use no-tracking queries by default (performance)      |

### Extension Method Parameters

```csharp
services.AddAetherweaveData<TDbContext>(
    configuration,                              // IConfiguration
    configure,                                  // Provider configuration delegate
    sectionName: "Aetherweave:DataRelational", // Optional: config section name
    addHealthCheck: true                        // Optional: add health check
);
```

## Advanced Usage

### Multiple DbContexts

```csharp
// First DbContext
services.AddAetherweaveData<OrderDbContext>(
    configuration,
    (builder, options) => builder.UseNpgsql(
        configuration.GetConnectionString("OrdersDb")),
    sectionName: "Aetherweave:OrdersDatabase");

// Second DbContext
services.AddAetherweaveData<InventoryDbContext>(
    configuration,
    (builder, options) => builder.UseNpgsql(
        configuration.GetConnectionString("InventoryDb")),
    sectionName: "Aetherweave:InventoryDatabase");
```

With separate configurations:

```json
{
  "ConnectionStrings": {
    "OrdersDb": "Host=localhost;Database=orders;...",
    "InventoryDb": "Host=localhost;Database=inventory;..."
  },
  "Aetherweave": {
    "OrdersDatabase": {
      "ConnectionStringName": "OrdersDb",
      "EnableDetailedErrors": true
    },
    "InventoryDatabase": {
      "ConnectionStringName": "InventoryDb",
      "EnableDetailedErrors": false
    }
  }
}
```

### Custom Provider Configuration

```csharp
services.AddAetherweaveData<ApplicationDbContext>(
    configuration,
    (builder, options) =>
    {
        var connString = configuration.GetConnectionString(options.ConnectionStringName);
        
        builder.UseNpgsql(connString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
                
            npgsqlOptions.MigrationsAssembly("MyApp.Migrations");
        });
    });
```

### Health Checks

Health checks are enabled by default. Access at `/health`:

```csharp
app.MapHealthChecks("/health");
```

Disable if not needed:

```csharp
services.AddAetherweaveData<ApplicationDbContext>(
    configuration,
    configure,
    addHealthCheck: false);
```

### Transactional Unit of Work - Error Handling

```csharp
public async Task ProcessPaymentAsync(PaymentCommand command, CancellationToken ct)
{
    await using var uow = uowFactory.CreateTransactional();
    
    try
    {
        var payment = Payment.Create(command.Amount);
        await paymentRepository.AddAsync(payment, ct);
        await uow.SaveChanges(ct);
        
        var invoice = Invoice.For(payment);
        await invoiceRepository.AddAsync(invoice, ct);
        await uow.SaveChanges(ct);
        
        await uow.Commit(ct);
    }
    catch (Exception)
    {
        await uow.Rollback(ct);  // Optional - happens automatically on dispose
        throw;
    }
}
```

### Explicit Rollback

```csharp
public async Task ProcessOrderAsync(ProcessOrderCommand command, CancellationToken ct)
{
    await using var uow = uowFactory.CreateTransactional();

    var order = await orderRepository.GetByIdAsync(command.OrderId, ct);

    order!.Process();
    orderRepository.Update(order);
    await uow.SaveChanges(ct);
    
    if (!await ValidateInventoryAsync(order, ct))
    {
        await uow.Rollback(ct);  // Explicit rollback
        throw new InsufficientInventoryException();
    }
    
    await uow.Commit(ct);
}
```

## Integration with CQRS

### Command Handler

```csharp
public sealed class CreateOrderHandler(
    IUnitOfWorkFactory uowFactory,
    IOrderRepository orderRepository,
    IDomainEventDispatcher eventDispatcher) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<ResponseWrapper<Guid>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        try
        {
            var order = Order.Create(
                Id<Order>.From(request.Id),
                Code<Order>.From(request.OrderNumber),
                request.CustomerId,
                request.Items);

            await orderRepository.AddAsync(order, cancellationToken);
            await uow.SaveChanges(cancellationToken);
            await uow.Commit(cancellationToken);
            
            // Dispatch domain events after successful commit
            await eventDispatcher.DispatchAsync(order, cancellationToken);
            
            return ResponseWrapper.Ok(order.Id);
        }
        catch (BusinessException ex)
        {
            return ResponseWrapper.Fail<Guid>(ErrorFactory.Create(ex));
        }
    }
}
```

### Query Handler (No Transaction Needed)

```csharp
public sealed class GetOrderByIdHandler(IOrderRepository orderRepository) 
    : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<ResponseWrapper<OrderDto>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            
        if (order is null)
        {
            var error = ErrorFactory.Create("ORDER_NOT_FOUND", "Order not found");
            return ResponseWrapper.Fail<OrderDto>(error);
        }
        
        return ResponseWrapper.Ok(OrderDto.FromDomain(order));
    }
}
```

## Repository Pattern

Repositories are the only place that accesses `DbContext`. They accept and return **domain models** publicly, and handle the mapping to and from **record models** internally.

```csharp
// Public interface - lives in the application or domain layer
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Id<Order> id, CancellationToken ct);
    Task<Order?> GetByCodeAsync(Code<Order> code, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
    void Update(Order order);
}

// Internal implementation - lives in the data layer; DbContext never leaves this class
internal sealed class OrderRepository(ApplicationDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Id<Order> id, CancellationToken ct)
    {
        var record = await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == (long)id, ct);

        return record is null ? null : OrderMapper.ToDomain(record);
    }

    public async Task<Order?> GetByCodeAsync(Code<Order> code, CancellationToken ct)
    {
        var record = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Code == (string)code, ct);

        return record is null ? null : OrderMapper.ToDomain(record);
    }

    public async Task AddAsync(Order order, CancellationToken ct)
        => await dbContext.Orders.AddAsync(OrderMapper.ToRecord(order), ct);

    public void Update(Order order)
        => dbContext.Orders.Update(OrderMapper.ToRecord(order));
}
```

Services and handlers only depend on the interface:

```csharp
public sealed class OrderService(IOrderRepository orderRepository, IUnitOfWorkFactory uowFactory)
{
    public async Task CreateOrderAsync(CreateOrderCommand command, CancellationToken ct)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        var order = Order.Create(command.Items);
        await orderRepository.AddAsync(order, ct);
        
        await uow.SaveChanges(ct);
        await uow.Commit(ct);
    }
}
```

## Best Practices

### ✅ DO

1. **Keep record models `internal`** - persistence types must not leak into the application or domain layers:
   ```csharp
   // Good
   internal sealed record OrderRecord(long Id, string Code, long CustomerId);
   internal DbSet<OrderRecord> Orders => Set<OrderRecord>();
   ```

2. **Keep `DbContext` inside repositories** - services and handlers inject `IXRepository`, never `DbContext`:
   ```csharp
   // Good - handler depends only on the repository interface
   public sealed class CreateOrderHandler(IUnitOfWorkFactory uowFactory, IOrderRepository orderRepository) { }
   
   // Bad - handler reaches into the data layer directly
   public sealed class CreateOrderHandler(IUnitOfWorkFactory uowFactory, ApplicationDbContext dbContext) { }
   ```

3. **Map at the repository boundary** - repositories translate between the domain model and the record:
   ```csharp
   public async Task AddAsync(Order order, CancellationToken ct)
       => await dbContext.Orders.AddAsync(OrderMapper.ToRecord(order), ct);
   ```

4. **Use transactional UoW for commands:**
   ```csharp
   await using var uow = uowFactory.CreateTransactional();
   await orderRepository.AddAsync(order, ct);
   await uow.SaveChanges(ct);
   await uow.Commit(ct);
   ```

5. **Dispatch domain events AFTER commit:**
   ```csharp
   await uow.Commit(ct);
   await eventDispatcher.DispatchAsync(aggregate, ct);
   ```

6. **Enable detailed errors only in development:**
   ```json
   {
     "Aetherweave": {
       "DataRelational": {
         "EnableDetailedErrors": true,  // Dev only!
         "EnableSensitiveDataLogging": false  // Never in production!
       }
     }
   }
   ```

### ❌ DON'T

1. **Don't inject `DbContext` into services or handlers:**
   ```csharp
   // Bad - DbContext leaks out of the data layer
   public sealed class OrderService(ApplicationDbContext dbContext) { }
   
   // Good - depends on the abstraction
   public sealed class OrderService(IOrderRepository orderRepository) { }
   ```

2. **Don't expose record models outside the data project:**
   ```csharp
   // Bad - leaks persistence concern into the application layer
   public async Task<OrderRecord?> GetByIdAsync(Id<Order> id, CancellationToken ct) { ... }
   
   // Good - return domain model; mapping stays in the repository
   public async Task<Order?> GetByIdAsync(Id<Order> id, CancellationToken ct) { ... }
   ```

3. **Don't forget to commit transactions:**
   ```csharp
   // Bad - transaction rolls back!
   await using var uow = uowFactory.CreateTransactional();
   await uow.SaveChanges(ct);
   // Forgot to call Commit()!
   
   // Good
   await using var uow = uowFactory.CreateTransactional();
   await uow.SaveChanges(ct);
   await uow.Commit(ct);  // ✅
   ```

4. **Don't use transactions for queries:**
   ```csharp
   // Bad - unnecessary transaction overhead
   await using var uow = uowFactory.CreateTransactional();
   var order = await orderRepository.GetByIdAsync(id, ct);
   
   // Good - no UoW needed for reads
   var order = await orderRepository.GetByIdAsync(id, ct);
   ```

5. **Don't enable sensitive data logging in production:**
   ```json
   // BAD in production - security risk!
   "EnableSensitiveDataLogging": true
   ```

6. **Don't dispatch events before commit:**
   ```csharp
   // Bad - events dispatched before data is persisted!
   await eventDispatcher.DispatchAsync(order, ct);
   await uow.Commit(ct);
   
   // Good
   await uow.Commit(ct);
   await eventDispatcher.DispatchAsync(order, ct);
   ```

## Exceptions

| Exception                              | When Thrown               | How to Handle                    |
|----------------------------------------|---------------------------|----------------------------------|
| `ConfigurationNotFoundException`       | Config section missing    | Check appsettings.json           |
| `TransactionAlreadyCommittedException` | Commit called twice       | Don't call Commit multiple times |
| `NoTransactionException`               | Commit before SaveChanges | Call SaveChanges first           |

## Environment-Specific Configuration

**Development (appsettings.Development.json):**

```json
{
  "Aetherweave": {
    "DataRelational": {
      "ConnectionStringName": "DefaultConnection",
      "EnableDetailedErrors": true,
      "EnableSensitiveDataLogging": true,
      "NoTrackingAsDefaultTrackingStrategy": false
    }
  }
}
```

**Production (appsettings.Production.json):**

```json
{
  "Aetherweave": {
    "DataRelational": {
      "ConnectionStringName": "DefaultConnection",
      "EnableDetailedErrors": false,
      "EnableSensitiveDataLogging": false,
      "NoTrackingAsDefaultTrackingStrategy": true
    }
  }
}
```

## Dependencies

- `Zwedze.Aetherweave.Data` - Unit of Work abstractions

## Migration from Raw EF Core

**Before (raw EF Core, single model, DbContext injected everywhere):**

```csharp
public class OrderService(ApplicationDbContext dbContext)
{
    public async Task CreateOrderAsync(Order order, CancellationToken ct)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(ct);
            
            var invoice = new Invoice(order.Id);
            dbContext.Invoices.Add(invoice);
            await dbContext.SaveChangesAsync(ct);
            
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
```

**After (Aetherweave, two models, DbContext hidden inside repositories):**

```csharp
// Data layer: DbContext stays here
internal sealed class OrderRepository(ApplicationDbContext dbContext) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct)
        => await dbContext.Orders.AddAsync(OrderMapper.ToRecord(order), ct);
}

// Application layer: service only knows about the repository interface
public sealed class OrderService(
    IUnitOfWorkFactory uowFactory,
    IOrderRepository orderRepository,
    IInvoiceRepository invoiceRepository)
{
    public async Task CreateOrderAsync(Order order, CancellationToken ct)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        await orderRepository.AddAsync(order, ct);
        await uow.SaveChanges(ct);
        
        await invoiceRepository.AddAsync(Invoice.For(order), ct);
        await uow.SaveChanges(ct);
        
        await uow.Commit(ct);
        // Auto-rollback if an exception is thrown or Commit is never called
    }
}
```

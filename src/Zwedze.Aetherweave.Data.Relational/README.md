# Aetherweave.Data.Relational

Clean, minimal EF Core wrapper with Unit of Work pattern for building maintainable data access layers.

## Features

- 🔄 **Unit of Work Pattern** - Transactional and non-transactional support
- ⚙️ **Auto-Configuration** - Configure DbContext from appsettings.json
- ⚡ **Auto-Starting Transactions** - No manual BeginTransaction needed
- 🏥 **Built-in Health Checks** - Ready for production monitoring
- 🛡️ **Provider Agnostic** - Works with any EF Core provider (PostgreSQL, SQL Server, SQLite, etc.)
- 🎯 **Clean Disposal** - Auto-rollback uncommitted transactions with warnings

## Installation

```bash
dotnet add package Zwedze.Aetherweave.Data.Relational
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL  # Or your preferred provider
```

## Quick Start

### 1. Define Your DbContext

```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
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

**Non-Transactional (Auto-commit):**

```csharp
public class OrderService(IUnitOfWorkFactory uowFactory, ApplicationDbContext dbContext)
{
    public async Task UpdateOrderStatus(Id<Order> orderId, OrderStatus status)
    {
        await using var uow = uowFactory.CreateNonTransactional();
        
        var order = await dbContext.Orders.FindAsync(orderId);
        order.UpdateStatus(status);
        
        await uow.SaveChanges();  // Commits immediately
    }
}
```

**Transactional (Explicit commit):**

```csharp
public class OrderService(IUnitOfWorkFactory uowFactory, ApplicationDbContext dbContext)
{
    public async Task CreateOrder(CreateOrderCommand command)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        // Transaction auto-starts on first SaveChanges
        var order = new Order(command.CustomerId, command.Items);
        dbContext.Orders.Add(order);
        await uow.SaveChanges();
        
        // Do more work...
        var invoice = new Invoice(order.Id, order.Total);
        dbContext.Invoices.Add(invoice);
        await uow.SaveChanges();
        
        // Explicitly commit - all or nothing
        await uow.Commit();
        
        // If you forget to commit, transaction auto-rolls back on dispose
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
    addHealthCheck: false);  // Disable health check
```

### Transactional Unit of Work - Error Handling

```csharp
public async Task ProcessPayment(PaymentCommand command)
{
    await using var uow = uowFactory.CreateTransactional();
    
    try
    {
        var payment = new Payment(command.Amount);
        dbContext.Payments.Add(payment);
        await uow.SaveChanges();
        
        var invoice = new Invoice(payment.Id);
        dbContext.Invoices.Add(invoice);
        await uow.SaveChanges();
        
        await uow.Commit();
    }
    catch (Exception)
    {
        // Automatic rollback on exception
        await uow.Rollback();  // Optional - happens automatically
        throw;
    }
}
```

### Explicit Rollback

```csharp
public async Task ProcessOrder(ProcessOrderCommand command)
{
    await using var uow = uowFactory.CreateTransactional();
    
    var order = await dbContext.Orders.FindAsync(command.OrderId);
    order.Process();
    await uow.SaveChanges();
    
    if (!await ValidateInventory(order))
    {
        await uow.Rollback();  // Explicit rollback
        throw new InsufficientInventoryException();
    }
    
    await uow.Commit();
}
```

## Integration with CQRS

### Command Handler

```csharp
public class CreateOrderHandler(
    IUnitOfWorkFactory uowFactory,
    ApplicationDbContext dbContext,
    IDomainEventDispatcher eventDispatcher) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<ResponseWrapper<Guid>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        try
        {
            var order = new Order(
                Id<Order>.From(request.Id),
                Code<Order>.From(request.OrderNumber),
                request.CustomerId);
                
            foreach (var item in request.Items)
            {
                order.AddItem(item.Product, item.Quantity);
            }
            
            dbContext.Orders.Add(order);
            await uow.SaveChanges(cancellationToken);
            await uow.Commit(cancellationToken);
            
            // Dispatch domain events after successful commit
            await eventDispatcher.DispatchAsync(order, cancellationToken);
            
            return ResponseWrapper<Guid>.Ok(order.Id);
        }
        catch (BusinessException ex)
        {
            return ResponseWrapper<Guid>.Fail(ErrorFactory.Create(ex));
        }
    }
}
```

### Query Handler (No Transaction Needed)

```csharp
public class GetOrderByIdHandler(ApplicationDbContext dbContext) 
    : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<ResponseWrapper<OrderDto>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        // No-tracking query (read-only)
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
            
        if (order is null)
        {
            var error = ErrorFactory.Create("ORDER_NOT_FOUND", "Order not found");
            return ResponseWrapper<OrderDto>.Fail(error);
        }
        
        var dto = OrderDto.FromEntity(order);
        return ResponseWrapper<OrderDto>.Ok(dto);
    }
}
```

## Repository Pattern

```csharp
public class OrderRepository(ApplicationDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Id<Order> id, CancellationToken ct)
    {
        return await dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Order?> GetByCodeAsync(Code<Order> orderNumber, CancellationToken ct)
    {
        return await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Code == orderNumber, ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct)
    {
        await dbContext.Orders.AddAsync(order, ct);
    }

    public void Update(Order order)
    {
        dbContext.Orders.Update(order);
    }
}

// Usage with Unit of Work
public class OrderService(
    IOrderRepository orderRepository,
    IUnitOfWorkFactory uowFactory)
{
    public async Task CreateOrder(CreateOrderCommand command)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        var order = new Order(command.Items);
        await orderRepository.AddAsync(order, CancellationToken.None);
        
        await uow.SaveChanges();
        await uow.Commit();
    }
}
```

## Best Practices

### ✅ DO

1. **Use transactional UoW for commands:**
   ```csharp
   await using var uow = uowFactory.CreateTransactional();
   // Make changes
   await uow.SaveChanges();
   await uow.Commit();  // Explicit commit
   ```

2. **Dispatch domain events AFTER commit:**
   ```csharp
   await uow.Commit();
   await eventDispatcher.DispatchAsync(aggregate, ct);
   ```

3. **Use no-tracking for read-only queries:**  
> The framework will automatically apply `AsNoTracking()` to all queries by default unless you explicitly disable it in the options.
   ```csharp
   var orders = await dbContext.Orders
       .AsNoTracking()  // Read-only performance boost
       .ToListAsync();
   ```

4. **Enable detailed errors only in development:**
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

1. **Don't forget to commit transactions:**
   ```csharp
   // Bad - transaction rolls back!
   await using var uow = uowFactory.CreateTransactional();
   await uow.SaveChanges();
   // Forgot to call Commit()!
   
   // Good
   await using var uow = uowFactory.CreateTransactional();
   await uow.SaveChanges();
   await uow.Commit();  // ✅
   ```

2. **Don't use transactions for queries:**
   ```csharp
   // Bad - unnecessary transaction
   await using var uow = uowFactory.CreateTransactional();
   var orders = await dbContext.Orders.ToListAsync();
   
   // Good - no UoW needed for queries
   var orders = await dbContext.Orders.AsNoTracking().ToListAsync();
   ```

3. **Don't enable sensitive data logging in production:**
   ```json
   // BAD in production - security risk!
   "EnableSensitiveDataLogging": true
   ```

4. **Don't dispatch events before commit:**
   ```csharp
   // Bad - events dispatched before data persisted!
   await eventDispatcher.DispatchAsync(order, ct);
   await uow.Commit();
   
   // Good
   await uow.Commit();
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

**Before (raw EF Core):**

```csharp
public class OrderService(ApplicationDbContext dbContext)
{
    public async Task CreateOrder(Order order)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
            
            var invoice = new Invoice(order.Id);
            dbContext.Invoices.Add(invoice);
            await dbContext.SaveChangesAsync();
            
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

**After (Aetherweave):**

```csharp
public class OrderService(
    IUnitOfWorkFactory uowFactory,
    ApplicationDbContext dbContext)
{
    public async Task CreateOrder(Order order)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        dbContext.Orders.Add(order);
        await uow.SaveChanges();
        
        var invoice = new Invoice(order.Id);
        dbContext.Invoices.Add(invoice);
        await uow.SaveChanges();
        
        await uow.Commit();
        // Auto-rollback if exception or forgot to commit
    }
}
```


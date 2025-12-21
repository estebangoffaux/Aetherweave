# Aetherweave.IdentityGenerators

Distributed, high-performance ID generation using Twitter Snowflake algorithm for creating unique, sortable identifiers
across multiple instances.

## Features

- ❄️ **Snowflake-Based IDs** - Generate 64-bit distributed unique identifiers
- 🏷️ **Type-Safe Integration** - Direct support for `Id<T>` and `Code<T>` from SharedKernel
- 🖥️ **Multi-Instance Support** - Machine name or IP-based instance identification
- ⏰ **Custom Epoch** - Configure your own epoch start date
- 🔄 **Clock Drift Protection** - Automatic retry on system clock issues
- 🎯 **Sequential & Sortable** - IDs are chronologically ordered
- ⚡ **High Performance** - Generate millions of IDs per second

## Installation

```bash
dotnet add package Zwedze.Aetherweave.IdentityGenerators
```

## How It Works

### Snowflake ID Structure

A 64-bit ID is composed of:

```
┌─────────────┬──────────┬────────────┐
│  Timestamp  │ Instance │  Sequence  │
│   41 bits   │  10 bits │   12 bits  │
└─────────────┴──────────┴────────────┘
```

**Benefits:**

- **Timestamp** - IDs are sortable by creation time
- **Instance ID** - Supports distributed systems (1024 instances with MachineName)
- **Sequence** - Multiple IDs per millisecond (4096 IDs/ms per instance)

## Quick Start

### 1. Register the Generator

**Program.cs:**

```csharp
services.AddAetherweaveGenerators(options =>
{
    options.InstanceIdType = InstanceIdType.MachineName;
    options.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
});
```

### 2. Inject and Use

```csharp
public class OrderService(IIdentityGenerator identityGenerator)
{
    public async Task<Order> CreateOrderAsync(CreateOrderCommand command)
    {
        var orderId = identityGenerator.CreateId<Order>();
        var orderCode = identityGenerator.CreateCode<Order>();
        
        var order = new Order(orderId, orderCode)
        {
            CustomerId = command.CustomerId,
            Items = command.Items
        };
        
        return order;
    }
}
```

### 3. Use in Aggregates

```csharp
public class Order : AggregateRoot<Order>
{
    public Order(Id<Order> id, Code<Order> code) : base(id, code)
    {
        CreatedAt = DateTime.UtcNow;
    }
    
    public DateTime CreatedAt { get; }
    public OrderStatus Status { get; private set; }
}
```

## Configuration Options

### Instance ID Strategies

#### 1. MachineName Strategy

Extracts instance ID from machine name suffix:

```csharp
services.AddAetherweaveGenerators(options =>
{
    options.InstanceIdType = InstanceIdType.MachineName;
});
```

**Requirements:**

- Machine name must end with a number (e.g., `WEBSERVER01`, `API-PROD-03`)
- Supports up to **1,024 instances** (10 bits)

**Examples:**

- `WEBSERVER01` → Instance ID: 1
- `API-PROD-123` → Instance ID: 123
- `LNXSRV03` → Instance ID: 3

**ID Structure:**

```
41 bits (timestamp) + 10 bits (instance) + 12 bits (sequence)
= Up to 1,024 instances
= Up to 4,096 IDs per millisecond per instance
```

#### 2. IP Address Strategy

Derives instance ID from last two octets of IP address:

```csharp
services.AddAetherweaveGenerators(options =>
{
    options.InstanceIdType = InstanceIdType.Ip;
});
```

**How it works:**

- Uses last two octets of IPv4 address
- Supports up to **65,536 instances** (16 bits)

**Examples:**

- `192.168.1.1` → Instance ID: 257 (binary: 00000001 00000001)
- `10.0.15.30` → Instance ID: 3870 (binary: 00001111 00011110)
- `172.16.0.100` → Instance ID: 100

**ID Structure:**

```
41 bits (timestamp) + 16 bits (instance) + 6 bits (sequence)
= Up to 65,536 instances
= Up to 64 IDs per millisecond per instance
```

### Custom Epoch

Configure a custom epoch to extend ID lifespan:

```csharp
services.AddAetherweaveGenerators(options =>
{
    options.InstanceIdType = InstanceIdType.MachineName;
    options.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
});
```

**Why Custom Epoch?**

- Default epoch (2019-01-01) gives ~69 years until overflow
- Custom recent epoch maximizes available time
- **Set epoch to your application's launch date**

**Lifespan Calculation:**
With 41 bits for timestamp:

- Maximum milliseconds: 2^41 = 2,199,023,255,552 ms
- Years: ~69.73 years from epoch

## API Reference

### IIdentityGenerator

```csharp
public interface IIdentityGenerator
{
    /// <summary>
    /// Generate a raw 64-bit unique identifier
    /// </summary>
    long GetNextUid();
    
    /// <summary>
    /// Generate a type-safe Id<T> for entity identification
    /// </summary>
    Id<T> CreateId<T>();
    
    /// <summary>
    /// Generate a UUID v7 Code<T> for business identifiers
    /// </summary>
    Code<T> CreateCode<T>();
}
```

### Methods Explained

#### GetNextUid()

Returns a raw 64-bit Snowflake ID:

```csharp
var uid = identityGenerator.GetNextUid();
// Returns: 1234567890123456789
```

**Use when:**

- Need raw long values
- Storing in non-Entity Framework contexts
- Interoperating with systems expecting long IDs

#### CreateId<T>()

Returns a type-safe `Id<T>` for entity identification:

```csharp
var orderId = identityGenerator.CreateId<Order>();
var customerId = identityGenerator.CreateId<Customer>();

// Compile-time safety
// orderId = customerId;  // ❌ Compile error!
```

**Use for:**

- Entity primary keys
- Database IDs
- Technical identifiers

#### CreateCode<T>()

Returns a `Code<T>` using UUID v7 (time-ordered GUID):

```csharp
var orderCode = identityGenerator.CreateCode<Order>();
// Returns: Code<Order> with value like "018d3f3b-7c4e-7000-8000-0123456789ab"
```

**Use for:**

- Order numbers
- Invoice numbers
- Reference codes
- Business identifiers visible to users

**Why UUID v7?**

- Time-ordered (sortable)
- Globally unique without coordination
- URL-safe string representation

## Advanced Usage

### Integration with Domain Entities

```csharp
public class Order : AggregateRoot<Order>
{
    // Private constructor for EF Core
    private Order() { }
    
    // Public factory method
    public static Order Create(
        IIdentityGenerator idGenerator,
        Id<Customer> customerId,
        List<OrderItem> items)
    {
        var id = idGenerator.CreateId<Order>();
        var code = idGenerator.CreateCode<Order>();
        
        var order = new Order(id, code)
        {
            CustomerId = customerId,
            Items = items,
            Status = OrderStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        
        order.RaiseDomainEvent(new OrderCreatedEvent(id, code));
        return order;
    }
    
    private Order(Id<Order> id, Code<Order> code) : base(id, code)
    {
    }
    
    public Id<Customer> CustomerId { get; private init; }
    public List<OrderItem> Items { get; private init; } = [];
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private init; }
}
```

### Integration with CQRS

```csharp
public class CreateOrderHandler(
    IIdentityGenerator identityGenerator,
    IOrderRepository orderRepository,
    IUnitOfWorkFactory uowFactory) : ICommandHandler<CreateOrderCommand, Id<Order>>
{
    public async Task<ResponseWrapper<Id<Order>>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        await using var uow = uowFactory.CreateTransactional();
        
        // Generate ID before creating entity
        var order = Order.Create(
            identityGenerator,
            request.CustomerId,
            request.Items);
        
        await orderRepository.AddAsync(order, cancellationToken);
        await uow.SaveChanges(cancellationToken);
        await uow.Commit(cancellationToken);
        
        return ResponseWrapper<Id<Order>>.Ok(order.Id);
    }
}
```

### Entity Framework Core Configuration

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        
        // Configure Id<Order> as long
        builder.Property(o => o.Id)
            .HasConversion(
                id => (long)id,
                value => (Id<Order>)value)
            .ValueGeneratedNever();  // IDs generated by application
        
        // Configure Code<Order> as string
        builder.Property(o => o.Code)
            .HasConversion(
                code => (string)code,
                value => (Code<Order>)value)
            .HasMaxLength(50)
            .IsRequired();
        
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.Code).IsUnique();
    }
}
```

### Multiple Entity Types

```csharp
public class ApplicationService(IIdentityGenerator identityGenerator)
{
    public async Task ProcessWorkflow()
    {
        // Each entity gets its own typed ID
        var orderId = identityGenerator.CreateId<Order>();
        var customerId = identityGenerator.CreateId<Customer>();
        var invoiceId = identityGenerator.CreateId<Invoice>();
        
        // Type safety prevents mixing
        // orderId = customerId;  // ❌ Compile error
        
        // Business codes are human-readable
        var orderCode = identityGenerator.CreateCode<Order>();
        var invoiceCode = identityGenerator.CreateCode<Invoice>();
        
        // orderCode = "ORD-2024-001";  // Format: UUID v7
        // invoiceCode = "INV-2024-001";
    }
}
```

## Best Practices

### ✅ DO

1. **Use MachineName for most scenarios:**
   ```csharp
   services.AddAetherweaveGenerators(options =>
   {
       options.InstanceIdType = InstanceIdType.MachineName;
   });
   ```

2. **Set custom epoch to application launch date:**
   ```csharp
   options.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
   ```

3. **Use CreateId<T> for database primary keys:**
   ```csharp
   var orderId = identityGenerator.CreateId<Order>();
   ```

4. **Use CreateCode<T> for user-visible identifiers:**
   ```csharp
   var orderNumber = identityGenerator.CreateCode<Order>();
   ```

5. **Generate IDs in application layer, not database:**
   ```csharp
   // Good - application generates ID
   var order = Order.Create(identityGenerator, customerId, items);
   
   // Bad - database auto-increment
   // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
   ```

6. **Configure EF Core to not generate IDs:**
   ```csharp
   builder.Property(o => o.Id).ValueGeneratedNever();
   ```

### ❌ DON'T

1. **Don't use IP strategy unless necessary:**
   ```csharp
   // Bad - fewer IDs per millisecond
   options.InstanceIdType = InstanceIdType.Ip;
   
   // Good - more IDs per millisecond
   options.InstanceIdType = InstanceIdType.MachineName;
   ```

2. **Don't use default epoch for new applications:**
   ```csharp
   // Bad - wastes time range
   // options.EpochStart = default;  // 2019-01-01
   
   // Good - maximizes available time
   options.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
   ```

3. **Don't mix raw longs with type-safe IDs:**
   ```csharp
   // Bad - loses type safety
   long orderId = identityGenerator.GetNextUid();
   
   // Good - type-safe
   Id<Order> orderId = identityGenerator.CreateId<Order>();
   ```

4. **Don't forget machine naming convention:**
   ```csharp
   // Machine names must end with numbers:
   // ✅ WEBSERVER01, API-PROD-123, LNXSRV03
   // ❌ WEBSERVER, API-PRODUCTION, LINUX
   ```

## Troubleshooting

### InstanceIdConversionFromMachineNameErrorException

**Error:**

```
The current machine name (WEBSERVER) is not in the format of <NAME><NUMBER>.
```

**Solution:**
Ensure machine names end with numbers:

```bash
# Windows
Rename-Computer -NewName "WEBSERVER01"

# Linux
hostnamectl set-hostname LNXSRV01
```

Or use IP strategy instead:

```csharp
options.InstanceIdType = InstanceIdType.Ip;
```

### UidGeneratorNoIpException

**Error:**

```
No network adapters with an IPv4 address in the system!
```

**Solution:**

- Verify network connectivity
- Check firewall settings
- Use MachineName strategy instead:
  ```csharp
  options.InstanceIdType = InstanceIdType.MachineName;
  ```

### Clock Drift / InvalidSystemClockException

**Automatic retry** - The generator automatically retries on clock issues:

```csharp
// Built-in retry policy handles this
_retryPolicy = Policy.Handle<InvalidSystemClockException>().RetryForever();
```

**Prevention:**

- Use NTP time synchronization
- Enable Windows Time Service
- Configure chrony/ntpd on Linux

## Distributed Systems Considerations

### Instance ID Limits

**MachineName Strategy:**

- Maximum: 1,024 instances (2^10)
- Instance IDs: 0-1023
- Best for: Most applications

**IP Strategy:**

- Maximum: 65,536 instances (2^16)
- Instance IDs: 0-65,535
- Best for: Very large deployments

### Clock Synchronization

**Critical:** All instances must have synchronized clocks (NTP).

**Why:** IDs from different instances must maintain global ordering.

**Setup NTP:**

```bash
# Ubuntu/Debian
sudo apt-get install ntp
sudo systemctl enable ntp
sudo systemctl start ntp

# Windows Server
w32tm /config /manualpeerlist:"time.windows.com" /syncfromflags:manual /reliable:YES /update
```

### Load Balancing

The generator works seamlessly behind load balancers:

```
┌─────────────┐
│ Load        │
│ Balancer    │
└──────┬──────┘
       │
   ┌───┴────┬───────┬────────┐
   ▼        ▼       ▼        ▼
┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐
│ API01│ │ API02│ │ API03│ │ API04│
│ ID:1 │ │ ID:2 │ │ ID:3 │ │ ID:4 │
└──────┘ └──────┘ └──────┘ └──────┘
```

Each instance generates unique IDs regardless of which handles the request.

## Migration from Auto-Increment IDs

### Before (Database Auto-Increment)

```csharp
public class Order
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }  // Generated by database
}
```

**Problems:**

- ❌ Can't know ID before insert
- ❌ Doesn't work across databases
- ❌ Race conditions in distributed systems
- ❌ Not type-safe

### After (Snowflake IDs)

```csharp
public class Order : AggregateRoot<Order>
{
    public Order(Id<Order> id, Code<Order> code) : base(id, code)
    {
    }
    
    public Id<Order> Id { get; }  // Generated by application
}
```

**Benefits:**

- ✅ Know ID before insert
- ✅ Works across multiple databases
- ✅ No coordination needed
- ✅ Type-safe with `Id<T>`

### Migration Steps

1. **Add generator to DI:**
   ```csharp
   services.AddAetherweaveGenerators(options =>
   {
       options.InstanceIdType = InstanceIdType.MachineName;
       options.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
   });
   ```

2. **Update entity:**
   ```csharp
   // Before
   public long Id { get; set; }
   
   // After
   public Id<Order> Id { get; }
   ```

3. **Update EF Core config:**
   ```csharp
   builder.Property(o => o.Id)
       .HasConversion(id => (long)id, value => (Id<Order>)value)
       .ValueGeneratedNever();  // Don't auto-generate!
   ```

4. **Generate IDs in application:**
   ```csharp
   var order = Order.Create(identityGenerator, customerId, items);
   ```

## Dependencies

- `IdGen` (3.0+) - Snowflake ID generation
- `Polly` (8.0+) - Retry policies

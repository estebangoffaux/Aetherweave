# Aetherweave.Generators

C# source generator that eliminates smart-enum boilerplate — classes whose instances are declared as static fields and identified by a `Code<T>`.

## Features

- **`[SmartEnum]` attribute** — opt-in marker injected into the consuming project at build time
- **`Code<T>` property** — typed business code for each instance
- **`FromCode(Code<T>)`** — look up an instance by its typed code; throws `ArgumentException` for unknown values
- **`FromCode(string)`** — convenience overload that wraps the string in `Code<T>`
- **`AllValues`** — static array of all declared instances in source order

## Installation

The generator ships as a `DevelopmentDependency` and runs automatically at build time:

```bash
dotnet add package Zwedze.Aetherweave.Generators
```

## Usage

### 1. Decorate a partial class and declare static members

```csharp
using Zwedze.Aetherweave;

[SmartEnum]
public sealed partial class OrderStatus
{
    public static readonly OrderStatus Draft     = new("draft");
    public static readonly OrderStatus Submitted = new("submitted");
    public static readonly OrderStatus Cancelled = new("cancelled");
}
```

**Requirements**

- The class must be `partial`. If not, warning `ZFCG003` is emitted and no code is generated.
- Static fields/properties must be of the **same type** as the enclosing class to be picked up.

### 2. What the generator emits

```csharp
// OrderStatus.SmartEnum.g.cs  (auto-generated)
public sealed partial class OrderStatus
{
    private OrderStatus(string code)
    {
        Code = Zwedze.Aetherweave.SharedKernel.Code<OrderStatus>.From(code);
    }

    public Zwedze.Aetherweave.SharedKernel.Code<OrderStatus> Code { get; }

    public static OrderStatus FromCode(Zwedze.Aetherweave.SharedKernel.Code<OrderStatus> code)
    {
        if (object.Equals(code, Draft.Code))     return Draft;
        if (object.Equals(code, Submitted.Code)) return Submitted;
        if (object.Equals(code, Cancelled.Code)) return Cancelled;
        throw new System.ArgumentException("Unknown OrderStatus code: " + code);
    }

    public static OrderStatus FromCode(string code) => FromCode(Code<OrderStatus>.From(code));

    public static OrderStatus[] AllValues => new OrderStatus[] { Draft, Submitted, Cancelled };
}
```

### 3. Consuming the generated API

```csharp
// Lookup by string
OrderStatus status = OrderStatus.FromCode("submitted");

// Lookup by typed code
Code<OrderStatus> code = Code<OrderStatus>.From("draft");
OrderStatus draft = OrderStatus.FromCode(code);

// Enumerate all values
foreach (OrderStatus s in OrderStatus.AllValues)
    Console.WriteLine(s.Code);

// Pattern-style comparison
if (status == OrderStatus.Submitted)
    Console.WriteLine("Awaiting review");
```

## Diagnostic

| ID        | Severity | Message                                                                          |
|-----------|----------|----------------------------------------------------------------------------------|
| `ZFCG003` | Warning  | `'{TypeName}' must be declared partial to generate the smart enum methods`       |

## How instance ordering works

Generated `AllValues` and `FromCode` match members in **source declaration order** (ordered by source span start position). Reordering static field declarations changes the order in `AllValues`.

## Target framework

The generator assembly targets **netstandard2.0** (required for Roslyn source generators) and is not included in the consuming project's build output. The `[SmartEnum]` attribute is injected into the consuming project's compilation at build time — no separate attribute package is needed.

## Dependencies

`Zwedze.Aetherweave.SharedKernel` must be referenced by the **consuming project** (not by the generator itself) for the generated `Code<T>` references to resolve.

# Aetherweave.Analyzers

Compile-time Roslyn analyzers that enforce uniqueness of `Id<T>` and `Code<T>` field values within classes and records.

## Features

- **IdDuplicationAnalyzer** - Build error when two fields in the same type share the same `Id<T>` literal value
- **CodeDuplicationAnalyzer** - Build error when two fields in the same type share the same `Code<T>` string value

## Installation

The analyzers ship as a `DevelopmentDependency` and apply automatically when the package is referenced. They never appear in the consuming project's build output.

```bash
dotnet add package Zwedze.Aetherweave.Analyzers
```

## What it detects

The analyzers target **field initializers** that use the `(Id<T>)literal` or `(Code<T>)"value"` cast patterns, the idiom used by smart-enum-style domain objects. Only duplicates **within the same type** are flagged.

### IdDuplication - error

```csharp
public sealed class OrderPriority
{
    public static readonly OrderPriority Low    = new((Id<OrderPriority>)1L);
    public static readonly OrderPriority Medium = new((Id<OrderPriority>)2L);
    public static readonly OrderPriority High   = new((Id<OrderPriority>)1L); // ❌ IdDuplication
}
```

### CodeDuplication - error

```csharp
public sealed class Currency
{
    public static readonly Currency Eur = new((Code<Currency>)"EUR");
    public static readonly Currency Usd = new((Code<Currency>)"USD");
    public static readonly Currency Dup = new((Code<Currency>)"EUR"); // ❌ CodeDuplication
}
```

## Diagnostics

| ID               | Severity | Category        | Message              |
|------------------|----------|-----------------|----------------------|
| `IdDuplication`   | Error    | Id Validation   | Id is duplicated     |
| `CodeDuplication` | Error    | Code Validation | Code is duplicated   |

## Scope and limitations

- Inspects `ClassDeclarationSyntax` and `RecordDeclarationSyntax` nodes.
- Only field declarations with an initializer using the explicit cast pattern are analysed.
- The first occurrence of a value is accepted; every subsequent duplicate is reported.
- Cross-type duplicates are not flagged; `Id<Order>` values are checked independently of `Id<Customer>` values.

## Target framework

The analyzer assembly targets **netstandard2.0** (required for Roslyn analyzers) and is not included in the consuming project's build output.

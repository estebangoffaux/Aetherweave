using Microsoft.EntityFrameworkCore;

namespace Zwedze.Aetherweave.Data.Relational;

public interface IDataRelationalDbProvider
{
    string Name { get; }

    void Configure(DbContextOptionsBuilder builder, DataRelationalOptions options);
}

internal sealed class DataRelationalDbProvider(string name, Action<DbContextOptionsBuilder, DataRelationalOptions> configure)
    : IDataRelationalDbProvider
{
    public string Name => name;

    public void Configure(DbContextOptionsBuilder builder, DataRelationalOptions options)
    {
        configure(builder, options);
    }
}

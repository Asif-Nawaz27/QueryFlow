using global::Dapper;

namespace QueryFlow.Dapper.Internal;

/// <summary>Hands out unique, safe parameter names and accumulates their values into a single shared <see cref="DynamicParameters"/> instance.</summary>
internal sealed class ParameterAllocator
{
    private int _counter;

    public DynamicParameters Parameters { get; } = new();

    /// <summary>Adds <paramref name="value"/> as a new parameter and returns its <c>@name</c> token for splicing into SQL text.</summary>
    public string Add(object? value)
    {
        var name = "qfp" + _counter++;
        Parameters.Add(name, value);
        return "@" + name;
    }
}

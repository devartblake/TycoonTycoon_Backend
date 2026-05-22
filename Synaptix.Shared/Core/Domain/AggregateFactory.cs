using System.Linq.Expressions;

namespace Synaptix.Shared.Core.Domain;

public static class AggregateFactory<T>
{
    private static readonly Func<T> _constructor = CreateTypeConstructor();

    private static Func<T> CreateTypeConstructor()
    {
        try
        {
            var newExpr = Expression.New(typeof(T));
            var func = Expression.Lambda<Func<T>>(newExpr);
            return func.Compile();
        }
        catch (ArgumentException)
        {
            return () => throw new System.Exception($"Aggregate {typeof(T).Name} does not have a parameterless constructor");
        }
    }

    public static T CreateAggregate()
    {
        return _constructor();
    }
}

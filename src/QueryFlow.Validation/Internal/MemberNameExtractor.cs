using System.Linq.Expressions;

namespace QueryFlow.Validation.Internal;

internal static class MemberNameExtractor
{
    /// <summary>Extracts the dotted property path from a lambda like <c>x => x.Address.City</c> or <c>x => x.Age</c> (value types are implicitly boxed via a Convert node, which is unwrapped).</summary>
    public static string GetPath<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        var body = expression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            body = unary.Operand;
        }

        var segments = new Stack<string>();
        while (body is MemberExpression memberExpression)
        {
            segments.Push(memberExpression.Member.Name);
            body = memberExpression.Expression;
        }

        if (segments.Count == 0)
        {
            throw new ArgumentException($"Expression '{expression}' must be a property access, e.g. x => x.PropertyName.", nameof(expression));
        }

        return string.Join('.', segments);
    }
}


using Language.Experimental.Expressions;
using Language.Experimental.Models;


namespace Language.Experimental.TypedExpressions;


public class TypedLiteralExpression : TypedExpression
{
    public object? Value { get; private set; }
    public TypedLiteralExpression(TypeInfo typeInfo, ExpressionBase originalExpression, object? value): base(typeInfo, originalExpression)
    {
        Value = value;
    }
}
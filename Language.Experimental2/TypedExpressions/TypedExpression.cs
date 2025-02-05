using Language.Experimental.Expressions;
using Language.Experimental.Models;

namespace Language.Experimental.TypedExpressions;


public class TypedExpression
{
    public TypeInfo TypeInfo { get; set; }
    public ExpressionBase OriginalExpression { get; set; }

    public TypedExpression(TypeInfo typeInfo, ExpressionBase originalExpression)
    {
        TypeInfo = typeInfo;
        OriginalExpression = originalExpression;
    }
}
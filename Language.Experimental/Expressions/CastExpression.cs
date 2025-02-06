using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Models;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class CastExpression : ExpressionBase
{
    public TypeInfo TypeInfo { get; private set; }
    public ExpressionBase Expression { get; private set; }
    public CastExpression(IToken token, TypeInfo typeInfo, ExpressionBase expression) : base(token)
    {
        TypeInfo = typeInfo;
        Expression = expression;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}
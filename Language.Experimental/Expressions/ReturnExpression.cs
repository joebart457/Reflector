using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class ReturnExpression : ExpressionBase
{
    public ExpressionBase? ReturnValue { get; set; }
    public ReturnExpression(IToken token, ExpressionBase? returnValue) : base(token)
    {
        ReturnValue = returnValue;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}
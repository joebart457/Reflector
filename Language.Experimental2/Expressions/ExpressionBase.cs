using Language.Experimental.Interfaces;
using Language.Experimental.TypedExpressions;
using Language.Experimental.TypeResolver;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public abstract class ExpressionBase
{
    public IToken Token { get; private set; }

    protected ExpressionBase(IToken token)
    {
        Token = token;
    }

    public abstract TypedExpression Evaluate(ITypeResolver typeResolver);
}
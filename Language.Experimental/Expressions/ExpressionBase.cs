using Language.Experimental.Compiler;
using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public abstract class ExpressionBase
{
    public IToken Token { get; private set; }

    protected ExpressionBase(IToken token)
    {
        Token = token;
    }

    public abstract TypedExpression Resolve(TypeResolver typeResolver);
}
using Language.Experimental.Compiler;
using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class CallExpression : ExpressionBase
{
    public ExpressionBase CallTarget { get; private set; }
    public List<ExpressionBase> Arguments { get; private set; }
    public CallExpression(IToken token, ExpressionBase callTarget, List<ExpressionBase> arguments) : base(token)
    {
        CallTarget = callTarget;
        Arguments = arguments;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}
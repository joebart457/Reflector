using Language.Experimental.Interfaces;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;
public class IdentifierExpression : ExpressionBase
{
    public IdentifierExpression(IToken token) : base(token)
    {
    }

    public override object? Evaluate(IInterpreter interpreter)
    {
        return interpreter.Evaluate(this);
    }
}

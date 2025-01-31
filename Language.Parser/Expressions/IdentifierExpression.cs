using Language.Parser.Interfaces;
using TokenizerCore.Interfaces;

namespace Language.Parser.Expressions;
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

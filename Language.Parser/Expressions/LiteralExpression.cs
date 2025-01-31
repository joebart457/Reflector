using Language.Parser.Interfaces;
using TokenizerCore.Interfaces;

namespace Language.Parser.Expressions;


public class LiteralExpression : ExpressionBase
{
    public object? Value { get; private set; }
    public LiteralExpression(IToken token, object? value): base(token)
    {
        Value = value;
    }

    public override object? Evaluate(IInterpreter interpreter)
    {
        return interpreter.Evaluate(this);
    }
}
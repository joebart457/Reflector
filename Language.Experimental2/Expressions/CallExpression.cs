using Language.Experimental.Interfaces;
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

    public override object? Evaluate(IInterpreter interpreter)
    {
        return interpreter.Evaluate(this);
    }
}
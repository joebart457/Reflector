using Language.Parser.Interfaces;
using TokenizerCore.Interfaces;

namespace Language.Parser.Expressions;

public abstract class ExpressionBase
{
    public IToken Token { get; private set; }

    protected ExpressionBase(IToken token)
    {
        Token = token;
    }

    public abstract object? Evaluate(IInterpreter interpreter);
}
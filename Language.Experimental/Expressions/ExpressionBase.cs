using Language.Experimental.Interfaces;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public abstract class ExpressionBase
{
    public IToken Token { get; private set; }

    private IToken? _startToken = null;
    private IToken? _endToken = null;
    public IToken StartToken { get => _startToken ?? throw new NullReferenceException(nameof(StartToken)); set => _startToken = value; }
    public IToken EndToken { get => _endToken ?? throw new NullReferenceException(nameof(EndToken)); set => _endToken = value; }

    protected ExpressionBase(IToken token)
    {
        Token = token;
    }

    public abstract TypedExpression Resolve(ITypeResolver typeResolver);
    public abstract ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap);

}
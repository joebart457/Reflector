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
    public IToken StartToken { get => _startToken ?? throw new NullReferenceException($"{nameof(StartToken)}, Expression type: {GetType().Name}"); set => _startToken = value; }
    public IToken EndToken { get => _endToken ?? throw new NullReferenceException($"{nameof(EndToken)}, Expression type: {GetType().Name}"); set => _endToken = value; }

    protected ExpressionBase(IToken token)
    {
        Token = token;
    }

    public abstract TypedExpression Resolve(ITypeResolver typeResolver);
    public abstract ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap);
    public ExpressionBase CopyStartAndEndTokens(ExpressionBase copyFrom)
    {
        _startToken = copyFrom.StartToken;
        _endToken = copyFrom.EndToken;
        return this;
    }

    public bool Contains(int line, int column)
    {
        if (line == StartToken.Start.Line && line == EndToken.End.Line) return StartToken.Start.Column <= column && EndToken.End.Column >= column;
        if (line == StartToken.Start.Line) return StartToken.Start.Column <= column;
        if (line == EndToken.End.Line) return EndToken.End.Column >= column;
        return StartToken.Start.Line <= line && EndToken.End.Line >= line;
    }

    public virtual bool TryGetContainingExpression(int line, int column, out ExpressionBase? containingExpression)
    {       
        if (Contains(line, column))
        {
            containingExpression = this;
            return true;
        }
        containingExpression = null;
        return false;
    }

}
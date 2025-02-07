using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;


public class UnresolvedLiteralExpression : UnresolvedExpressionBase
{
    public object? Value { get; private set; }
    public UnresolvedLiteralExpression(IToken token, object? value): base(token)
    {
        Value = value;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedLiteralExpression(Token, Value);
    }
}
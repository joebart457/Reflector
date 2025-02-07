using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

public class UnresolvedCastExpression : UnresolvedExpressionBase
{
    public TypeSymbol TypeSymbol { get; private set; }
    public UnresolvedExpressionBase Expression { get; private set; }
    public UnresolvedCastExpression(IToken token, TypeSymbol typeSymbol, UnresolvedExpressionBase expression) : base(token)
    {
        TypeSymbol = typeSymbol;
        Expression = expression;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedCastExpression(Token, TypeSymbol.ReplaceGenericTypeParameter(genericToConcreteTypeMap), Expression.ReplaceGenericTypeSymbols(genericToConcreteTypeMap));
    }
}
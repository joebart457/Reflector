using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

public class UnresolvedReturnExpression : UnresolvedExpressionBase
{
    public UnresolvedExpressionBase? ReturnValue { get; set; }
    public UnresolvedReturnExpression(IToken token, UnresolvedExpressionBase? returnValue) : base(token)
    {
        ReturnValue = returnValue;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedReturnExpression(Token, ReturnValue?.ReplaceGenericTypeSymbols(genericToConcreteTypeMap));
    }
}